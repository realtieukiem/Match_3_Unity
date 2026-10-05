using System;
using System.Collections;
using System.Collections.Generic;
using Pokiwar.App;
using Pokiwar.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Pokiwar.UI
{
    /// <summary>Drives one battle: ticks the TurnClock, forwards player input / AI decisions to BattleEngine, and replays the returned events.</summary>
    public sealed class BattleController : MonoBehaviour
    {
        [Header("Views")]
        public BoardView Board;
        public CombatantHud PlayerHud;
        public CombatantHud EnemyHud;
        public ActionButtonView[] CardButtons = new ActionButtonView[5];
        public ActionButtonView[] SkillButtons = new ActionButtonView[2];
        public GemSummaryView Summary;
        public FloatingTextLayer Floating;
        public QteView Qte;
        public EventLogView Log;
        public Text TimerLabel;
        public Image TimerFill;
        public Text TurnLabel;
        public GameObject ArrowToPlayer;
        public GameObject ArrowToEnemy;
        public CanvasGroup Banner;
        public Text BannerLabel;
        public Text EncounterLabel;
        public Text EnemyKitLabel;
        public Button AutoButton;
        public Text AutoLabel;
        public Button GiveUpButton;
        public ScreenShake Shake;

        [Header("Tuning")]
        [Tooltip("Animation speed multiplier (1 = normal).")]
        public float AnimationSpeed = 1f;
        [Tooltip("Let the AI play the player's side too (testing).")]
        public bool AutoPlay;
        [Tooltip("Also print every CombatEvent to the Console.")]
        public bool LogToConsole;

        public BattleEngine Engine { get; private set; }
        public TurnClock Clock { get; private set; }
        public bool Running { get; private set; }

        private SpriteLibrary sprites;
        private Action<BattleReport> finished;
        private BattleReport report;
        private SeededRng aiRng;
        private bool reported;
        private bool giveUp;
        private int lastTick = -1;

        private enum PendingKind { None, Swap, Card, Skill }
        private PendingKind pending;
        private Pos pendingA, pendingB;
        private int pendingIndex;

        private void Awake()
        {
            Board.SwapRequested += (a, b) => Queue(PendingKind.Swap, a, b, 0);
            Board.StepCleared += (step, index) =>
            {
                if (index >= 1 && Floating != null) Floating.Spawn("COMBO x" + (index + 1) + "!", new Color(1f, 0.9f, 0.3f), Board.GemRoot, new Vector2(0, 250), 52);
            };
            for (int i = 0; i < CardButtons.Length; i++)
            {
                int k = i;
                CardButtons[i].Button.onClick.AddListener(() => Queue(PendingKind.Card, default, default, k));
            }
            for (int i = 0; i < SkillButtons.Length; i++)
            {
                int k = i;
                SkillButtons[i].Button.onClick.AddListener(() => Queue(PendingKind.Skill, default, default, k));
            }
            AutoButton.onClick.AddListener(() =>
            {
                AutoPlay = !AutoPlay;
                RefreshAutoLabel();
            });
            GiveUpButton.onClick.AddListener(() => giveUp = true);
            if (Banner != null) Banner.alpha = 0f;
        }

        public void Begin(BattleSetup setup, EncounterDef encounter, string nodeId, string petUid, SpriteLibrary spriteLibrary, Action<BattleReport> onFinished)
        {
            StopAllCoroutines();
            sprites = spriteLibrary;
            finished = onFinished;
            reported = false;
            giveUp = false;
            pending = PendingKind.None;
            Tween.Speed = AnimationSpeed;
            lastTick = -1;
            Shake?.Stop();
            VfxLayer.Instance?.Clear();
            AudioDirector.Instance?.PlayMusic(encounter.IsBoss ? "music.boss" : "music.battle");
            Engine = new BattleEngine(setup);
            Clock = new TurnClock(setup.Board.TurnSeconds);
            aiRng = new SeededRng(setup.Seed ^ 0xA1A1A1u);
            report = new BattleReport { BattleId = setup.BattleId, EncounterId = encounter.Id, NodeId = nodeId, PetUid = petUid };
            var s = Engine.State;
            PlayerHud.Setup(s.Get(Side.Player), sprites, true);
            EnemyHud.Setup(s.Get(Side.Enemy), sprites, false);
            Board.Sprites = sprites;
            Board.Rebuild(s.Board);
            Board.InputEnabled = false;
            Log.Clear();
            Log.Add("Battle " + setup.BattleId + " seed " + setup.Seed);
            EncounterLabel.text = encounter.Name + (encounter.IsBoss ? "  [BOSS]" : "");
            EnemyKitLabel.text = DescribeKit(s.Get(Side.Enemy));
            RefreshAutoLabel();
            RefreshActionBar();
            StartCoroutine(MainLoop());
        }

        private static string DescribeKit(Combatant c)
        {
            var parts = new List<string>();
            foreach (var sk in c.Skills) parts.Add(sk.Name);
            foreach (var cs in c.Cards) parts.Add(cs.Def.Name);
            if (c.Phases.Count > 0) parts.Add(c.Phases.Count + 1 + " forms");
            return parts.Count == 0 ? "" : "Enemy kit: " + string.Join(", ", parts);
        }

        private void RefreshAutoLabel()
        {
            if (AutoLabel != null) AutoLabel.text = AutoPlay ? "AUTO: ON" : "AUTO: OFF";
        }

        private void Queue(PendingKind kind, Pos a, Pos b, int index)
        {
            if (!Running || Engine == null || !Engine.IsTurnOf(Side.Player) || AutoPlay) return;
            if (pending != PendingKind.None) return;
            pending = kind;
            pendingA = a;
            pendingB = b;
            pendingIndex = index;
        }

        private void Update()
        {
            if (Clock == null) return;
            int sec = Mathf.CeilToInt(Clock.Remaining);
            TimerLabel.text = sec.ToString();
            if (Clock.Running && sec != lastTick)
            {
                if (sec <= 3 && sec > 0 && Engine != null && Engine.IsTurnOf(Side.Player) && !AutoPlay)
                {
                    AudioDirector.Sfx("tick", sec == 1 ? 1.5f : 1.2f, 1f, false);
                    StartCoroutine(CombatantHud.Pop(TimerLabel.rectTransform, 1.4f));
                }
                lastTick = sec;
            }
            TimerLabel.color = Clock.Running && sec <= 3 ? new Color(1f, 0.45f, 0.4f) : Color.white;
            if (TimerFill != null) TimerFill.fillAmount = Clock.Duration <= 0 ? 0 : Clock.Remaining / Clock.Duration;
            Tween.Speed = AnimationSpeed;
        }

        private IEnumerator MainLoop()
        {
            Running = true;
            AudioDirector.Sfx("fight");
            yield return ShowBanner("FIGHT!", 0.8f);
            var s = Engine.State;
            while (!s.Ended && !giveUp)
            {
                var start = Engine.BeginTurn();
                SetTurnVisuals(s.Current);
                yield return Play(start);
                if (s.Ended) break;
                Clock.Restart();
                RefreshActionBar();
                if (s.Current == Side.Player && !AutoPlay) yield return PlayerTurn();
                else yield return AiTurn(s.Current);
                Board.InputEnabled = false;
                Clock.Freeze();
                if (s.Ended || giveUp) break;
                Engine.EndTurn();
                yield return Tween.Wait(0.2f);
            }
            Running = false;
            Board.InputEnabled = false;
            Clock.Freeze();
            bool won = !giveUp && s.Ended && s.Winner == Side.Player;
            AudioDirector.Instance?.StopMusic();
            AudioDirector.Sfx(won ? "victory" : "defeat", 1f, 1f, false);
            if (won && VfxLayer.Instance != null)
            {
                var v = VfxLayer.Instance;
                v.Burst(PlayerHud.FloatAnchor.position, new Color(1f, 0.85f, 0.3f), 30, 1000f, 22f, 1.2f, 900f, v.Star, 70f, 70f);
                v.Burst(EnemyHud.FloatAnchor.position, new Color(0.5f, 0.9f, 1f), 30, 1000f, 22f, 1.2f, 900f, v.Star, 70f, 110f);
            }
            yield return ShowBanner(won ? "VICTORY!" : "DEFEAT", 1.0f);
            report.Won = won;
            report.Turns = s.TurnNumber;
            if (!reported)
            {
                reported = true;
                finished?.Invoke(report);
            }
        }

        private IEnumerator PlayerTurn()
        {
            var s = Engine.State;
            pending = PendingKind.None;
            Board.InputEnabled = true;
            while (!s.Ended && !giveUp)
            {
                if (AutoPlay)
                {
                    Board.InputEnabled = false;
                    yield return AiTurn(Side.Player);
                    yield break;
                }
                if (Clock.Tick(Time.deltaTime))
                {
                    Board.InputEnabled = false;
                    pending = PendingKind.None;
                    var to = Engine.Timeout(Side.Player);
                    yield return Play(to);
                    yield break;
                }
                if (pending == PendingKind.None)
                {
                    yield return null;
                    continue;
                }
                var kind = pending;
                Board.InputEnabled = false;
                ActionResult r = null;
                if (kind == PendingKind.Swap)
                {
                    r = Engine.Swap(Side.Player, pendingA, pendingB);
                    if (!r.Accepted)
                    {
                        float t0 = Time.time;
                        if (r.Board != null) yield return Board.AnimateSwap(pendingA, pendingB, false);
                        else Toast(r.RejectReason);
                        pending = PendingKind.None;
                        if (Clock.Tick(Time.time - t0))
                        {
                            yield return Play(Engine.Timeout(Side.Player));
                            yield break;
                        }
                        Board.InputEnabled = true;
                        continue;
                    }
                    Clock.Freeze();
                    yield return Play(r);
                }
                else if (kind == PendingKind.Card)
                {
                    Clock.Freeze();
                    r = Engine.UseCard(Side.Player, pendingIndex);
                    if (!r.Accepted) Toast(r.RejectReason);
                    else yield return Play(r);
                }
                else if (kind == PendingKind.Skill)
                {
                    Clock.Freeze();
                    var check = Engine.PrepareSkill(Side.Player, pendingIndex);
                    if (!check.Ok) Toast(check.Reason);
                    else
                    {
                        QteResult qr = QteResult.None;
                        yield return Qte.Run(check, Engine.State.Get(Side.Player).FormName, null, q => qr = q);
                        r = Engine.UseSkill(Side.Player, pendingIndex, qr);
                        if (!r.Accepted) Toast(r.RejectReason);
                        else yield return Play(r);
                    }
                }
                pending = PendingKind.None;
                RefreshActionBar();
                if (r != null && r.Accepted && (r.EndsTurn || s.Ended)) yield break;
                Clock.Resume();
                Board.InputEnabled = true;
            }
        }

        private IEnumerator AiTurn(Side side)
        {
            var s = Engine.State;
            var me = s.Get(side);
            var policy = me.Ai ?? new AIPolicy();
            float think = Engine.Rules.AiThinkSeconds;
            float waited = 0f;
            while (waited < think / Mathf.Max(0.01f, Tween.Speed))
            {
                waited += Time.deltaTime;
                if (Clock.Tick(Time.deltaTime))
                {
                    yield return Play(Engine.Timeout(side));
                    yield break;
                }
                yield return null;
            }
            Clock.Freeze();
            var d = AIPlanner.Decide(Engine, side, policy, aiRng);
            if (LogToConsole) Debug.Log("[Pokiwar AI] " + side + " " + d.Reason);
            foreach (var idx in d.CardsBeforeMatch)
            {
                var r = Engine.UseCard(side, idx);
                if (!r.Accepted) continue;
                yield return Play(r);
                if (s.Ended || r.EndsTurn) yield break;
                yield return Tween.Wait(0.2f);
            }
            if (d.SkillIndex >= 0)
            {
                var check = Engine.PrepareSkill(side, d.SkillIndex);
                if (check.Ok)
                {
                    var roll = AIPlanner.RollQte(policy, check.Skill.Qte, aiRng);
                    QteResult qr = roll;
                    yield return Qte.Run(check, me.FormName, roll, q => qr = q);
                    var r = Engine.UseSkill(side, d.SkillIndex, qr);
                    if (r.Accepted)
                    {
                        yield return Play(r);
                        if (r.EndsTurn || s.Ended) yield break;
                    }
                }
                d = AIPlanner.Decide(Engine, side, new AIPolicy { UseCards = false, UseSkills = false }, aiRng);
            }
            if (d.HasSwap)
            {
                Board.Hint(d.Swap);
                yield return Tween.Wait(0.25f);
                var r = Engine.Swap(side, d.Swap.A, d.Swap.B);
                if (r.Accepted)
                {
                    yield return Play(r);
                    yield break;
                }
            }
            yield return Play(Engine.Timeout(side));
        }

        private IEnumerator Play(ActionResult r)
        {
            if (r == null) yield break;
            if (r.Board != null && r.Board.Valid)
            {
                yield return Board.AnimateResolution(r.Board, Engine.State.Board);
                yield return Summary.Show(r.Actor, r.Board.Tally);
            }
            foreach (var ev in r.Events)
            {
                Log.Add(ev.ToString());
                if (LogToConsole) Debug.Log("[Pokiwar] " + ev);
                yield return PlayEvent(ev);
            }
            if (r.Board != null && r.Board.Valid) yield return Summary.Hide();
            PlayerHud.Set(Engine.State.Get(Side.Player).Snapshot());
            EnemyHud.Set(Engine.State.Get(Side.Enemy).Snapshot());
            PlayerHud.SetStatus(Engine.State.Get(Side.Player));
            EnemyHud.SetStatus(Engine.State.Get(Side.Enemy));
            if (!Board.Matches(Engine.State.Board)) Board.Rebuild(Engine.State.Board);
            RefreshActionBar();
        }

        private CombatantHud Hud(Side s) => s == Side.Player ? PlayerHud : EnemyHud;

        private IEnumerator PlayEvent(CombatEvent ev)
        {
            var actorHud = Hud(ev.Actor);
            var targetHud = Hud(ev.Target);
            var vfx = VfxLayer.Instance;
            switch (ev.Kind)
            {
                case CombatEventKind.TurnSkipped:
                    AudioDirector.Sfx("timeout");
                    Float(actorHud, "TIME OUT", Color.gray, 0);
                    yield return Tween.Wait(0.4f);
                    break;
                case CombatEventKind.GemEffect:
                case CombatEventKind.ResourceChange:
                {
                    bool fromGem = ev.Kind == CombatEventKind.GemEffect;
                    if (fromGem && vfx != null && Board.LastCleared.TryGetValue(ev.Gem, out var cells) && cells.Count > 0)
                    {
                        float wait = vfx.Orbs(Sample(cells, 6), targetHud.BarRect(ev.Resource).position, ResourceColor(ev.Resource), 2);
                        yield return Tween.Wait(wait);
                    }
                    targetHud.Set(ev.TargetAfter);
                    actorHud.Set(ev.ActorAfter);
                    if (ev.Applied != 0 || ev.Computed != 0)
                    {
                        string label = (ev.Applied >= 0 ? "+" : "") + ev.Applied + " " + ResourceShort(ev.Resource);
                        if (ev.Applied != ev.Computed && ev.Computed > 0) label += " (" + ev.Computed + ")";
                        Float(targetHud, label, ResourceColor(ev.Resource), ev.SourceId == "turn-start" ? 30 : 40);
                        if (ev.Applied > 0 && ev.SourceId != "turn-start" && ev.SourceId != "hp-damage") GainFeedback(targetHud, ev.Resource);
                        yield return Tween.Wait(fromGem ? 0.3f : 0.15f);
                    }
                    break;
                }
                case CombatEventKind.Steal:
                    AudioDirector.Sfx("steal");
                    if (vfx != null && ev.Applied > 0)
                    {
                        var from = new List<Vector3> { targetHud.BarRect(ev.Resource).position };
                        float wait = vfx.Orbs(from, actorHud.BarRect(ev.Resource).position, new Color(0.85f, 0.85f, 0.95f), 6);
                        yield return Tween.Wait(wait);
                        actorHud.PopBar(ev.Resource);
                    }
                    actorHud.Set(ev.ActorAfter);
                    targetHud.Set(ev.TargetAfter);
                    Float(targetHud, "-" + ev.Applied + " " + ResourceShort(ev.Resource) + (ev.Applied < ev.Computed ? " (" + ev.Computed + ")" : ""), new Color(0.85f, 0.85f, 0.85f), 36);
                    Float(actorHud, "STEAL " + ResourceShort(ev.Resource), Color.white, 32);
                    yield return Tween.Wait(0.3f);
                    break;
                case CombatEventKind.Attack:
                    if (ev.Gem == GemType.Sword && vfx != null && Board.LastCleared.TryGetValue(GemType.Sword, out var swords) && swords.Count > 0)
                    {
                        float wait = vfx.Orbs(Sample(swords, 6), targetHud.FloatAnchor.position, SpriteLibrary.GemColor(GemType.Sword), 2, 0.36f, 30f);
                        AudioDirector.Sfx("swing");
                        yield return Tween.Wait(wait * 0.7f);
                    }
                    else AudioDirector.Sfx("swing");
                    yield return actorHud.Lunge();
                    break;
                case CombatEventKind.Damage:
                {
                    yield return Tween.Wait(ev.Strong ? 0.11f : 0.05f);
                    targetHud.Set(ev.TargetAfter);
                    actorHud.Set(ev.ActorAfter);
                    var at = targetHud.FloatAnchor.position;
                    bool big = ev.Strong || ev.SourceId != null && ev.SourceId.StartsWith("skill:");
                    AudioDirector.Sfx(big ? "hit.strong" : "hit");
                    if (ev.ShieldAbsorbed > 0)
                    {
                        AudioDirector.Sfx("block");
                        vfx?.Ring(at, new Color(0.75f, 0.5f, 1f, 0.9f), 320f, 0.35f);
                    }
                    if (vfx != null)
                    {
                        vfx.Burst(at, big ? new Color(1f, 0.6f, 0.15f) : new Color(1f, 0.85f, 0.6f), big ? 28 : 12, big ? 900f : 650f, big ? 26f : 20f, 0.5f, 700f, vfx.Star);
                        if (big)
                        {
                            vfx.Ring(at, new Color(1f, 0.8f, 0.4f, 0.9f), 520f, 0.4f);
                            vfx.ScreenFlash(Color.white, 0.3f, 0.2f);
                        }
                    }
                    float ratio = targetHud == null || ev.TargetAfter.MaxHp <= 0 ? 0f : (float)ev.Applied / ev.TargetAfter.MaxHp;
                    Shake?.Add(Mathf.Clamp((big ? 0.55f : 0.25f) + ratio * 2f, 0f, 0.95f));
                    string dmg = (ev.Strong ? "RAGE! " : "") + "-" + ev.Applied;
                    if (ev.ShieldAbsorbed > 0) dmg += "  [shield -" + ev.ShieldAbsorbed + "]";
                    Float(targetHud, dmg, ev.Strong ? new Color(1f, 0.55f, 0.1f) : new Color(1f, 0.3f, 0.3f), big ? 56 : 46);
                    yield return targetHud.Shake();
                    break;
                }
                case CombatEventKind.CardUsed:
                    AudioDirector.Sfx("card");
                    vfx?.Ring(actorHud.FloatAnchor.position, new Color(0.45f, 0.7f, 1f, 0.9f), 380f, 0.4f);
                    yield return ShowBanner((ev.Actor == Side.Player ? "" : "Enemy: ") + ev.Text, 0.6f);
                    break;
                case CombatEventKind.SkillUsed:
                    AudioDirector.Sfx("skill");
                    if (vfx != null)
                    {
                        vfx.Ring(actorHud.FloatAnchor.position, new Color(1f, 0.75f, 0.3f, 0.95f), 460f, 0.45f);
                        vfx.Burst(actorHud.FloatAnchor.position, new Color(1f, 0.7f, 0.25f), 18, 500f, 22f, 0.6f, -200f, vfx.Star);
                    }
                    yield return ShowBanner((ev.Actor == Side.Player ? "" : "Enemy: ") + ev.Text + "!", 0.6f);
                    break;
                case CombatEventKind.QteResolved:
                    Float(actorHud, ev.Text + "  " + ev.Computed, Color.yellow, 34);
                    AudioDirector.Sfx("swing");
                    yield return actorHud.Lunge();
                    break;
                case CombatEventKind.Buff:
                    AudioDirector.Sfx("buff");
                    vfx?.Ring(targetHud.FloatAnchor.position, ev.Actor == ev.Target ? new Color(0.5f, 0.9f, 1f, 0.9f) : new Color(1f, 0.4f, 0.4f, 0.9f), 380f, 0.4f);
                    targetHud.SetStatus(Engine.State.Get(ev.Target));
                    Float(targetHud, ev.Text, new Color(0.6f, 0.9f, 1f), 32);
                    yield return Tween.Wait(0.3f);
                    break;
                case CombatEventKind.SummonCreated:
                    AudioDirector.Sfx("summon");
                    vfx?.Burst(actorHud.FloatAnchor.position, new Color(0.6f, 1f, 0.6f), 16, 400f, 20f, 0.7f, -250f, vfx.Star);
                    actorHud.SetStatus(Engine.State.Get(ev.Actor));
                    Float(actorHud, "Summon: " + ev.Text, new Color(0.7f, 1f, 0.7f), 32);
                    yield return Tween.Wait(0.3f);
                    break;
                case CombatEventKind.SummonHit:
                    Float(actorHud, ev.Text + " attacks!", new Color(0.7f, 1f, 0.7f), 30);
                    yield return Tween.Wait(0.25f);
                    break;
                case CombatEventKind.PhaseTriggered:
                {
                    actorHud.Set(ev.ActorAfter);
                    var at = actorHud.FloatAnchor.position;
                    AudioDirector.Instance?.Duck(-12f, 1.6f);
                    AudioDirector.Sfx("transform");
                    yield return ShowBanner(ev.Text + " transforms!", 0.4f);
                    if (vfx != null)
                    {
                        vfx.ScreenFlash(Color.white, 0.75f, 0.45f);
                        vfx.Ring(at, new Color(0.5f, 0.95f, 1f, 1f), 900f, 0.6f);
                        vfx.Burst(at, new Color(0.4f, 0.9f, 1f), 40, 1100f, 30f, 0.8f, 500f, vfx.Star);
                    }
                    Shake?.Add(0.9f);
                    yield return actorHud.Transform(sprites.Get(ev.ActorAfter.SpriteKey));
                    vfx?.Burst(at, new Color(1f, 0.9f, 0.5f), 20, 300f, 18f, 0.9f, -300f, vfx.Star);
                    Float(actorHud, "HP " + ev.Before + " -> " + ev.After, new Color(0.5f, 1f, 0.6f), 38);
                    EnemyKitLabel.text = DescribeKit(Engine.State.Get(Side.Enemy));
                    yield return Tween.Wait(0.6f);
                    break;
                }
                case CombatEventKind.Death:
                    AudioDirector.Sfx("death");
                    vfx?.Burst(actorHud.FloatAnchor.position, new Color(0.8f, 0.8f, 0.85f), 36, 800f, 26f, 0.9f, 800f);
                    Shake?.Add(0.5f);
                    yield return actorHud.Die();
                    break;
                case CombatEventKind.Reshuffled:
                    Float(PlayerHud, "Board reshuffled", Color.white, 30);
                    break;
            }
        }

        private void GainFeedback(CombatantHud hud, ResourceKind k)
        {
            hud.PopBar(k);
            var vfx = VfxLayer.Instance;
            var at = hud.BarRect(k).position;
            switch (k)
            {
                case ResourceKind.Hp:
                    AudioDirector.Sfx("heal");
                    vfx?.Burst(hud.FloatAnchor.position, new Color(0.45f, 1f, 0.5f), 14, 260f, 18f, 0.8f, -260f, vfx.Star);
                    break;
                case ResourceKind.Mana:
                    AudioDirector.Sfx("mana");
                    vfx?.Burst(at, new Color(0.45f, 0.7f, 1f), 8, 260f, 14f, 0.45f, -100f);
                    break;
                case ResourceKind.Rage:
                    AudioDirector.Sfx("rage");
                    vfx?.Burst(at, new Color(1f, 0.55f, 0.2f), 8, 300f, 14f, 0.45f, -200f);
                    break;
                case ResourceKind.Shield:
                    AudioDirector.Sfx("shield");
                    vfx?.Ring(hud.FloatAnchor.position, new Color(0.75f, 0.5f, 1f, 0.85f), 420f, 0.45f);
                    break;
            }
        }

        private static List<Vector3> Sample(List<Vector3> src, int max)
        {
            if (src.Count <= max) return src;
            var r = new List<Vector3>(max);
            for (int i = 0; i < max; i++) r.Add(src[i * src.Count / max]);
            return r;
        }
        private void Float(CombatantHud hud, string text, Color c, int size)
        {
            if (Floating != null) Floating.Spawn(text, c, hud.FloatAnchor, new Vector2(UnityEngine.Random.Range(-40f, 40f), 0), size == 0 ? 40 : size);
        }

        private static string ResourceShort(ResourceKind k)
        {
            switch (k)
            {
                case ResourceKind.Hp: return "HP";
                case ResourceKind.Mana: return "MP";
                case ResourceKind.Rage: return "RAGE";
                case ResourceKind.Shield: return "SHIELD";
                default: return "";
            }
        }

        private static Color ResourceColor(ResourceKind k)
        {
            switch (k)
            {
                case ResourceKind.Hp: return new Color(0.4f, 1f, 0.45f);
                case ResourceKind.Mana: return new Color(0.45f, 0.7f, 1f);
                case ResourceKind.Rage: return new Color(1f, 0.5f, 0.3f);
                case ResourceKind.Shield: return new Color(0.8f, 0.55f, 1f);
                default: return Color.white;
            }
        }

        private void SetTurnVisuals(Side s)
        {
            PlayerHud.SetTurn(s == Side.Player);
            EnemyHud.SetTurn(s == Side.Enemy);
            if (ArrowToPlayer != null) ArrowToPlayer.SetActive(s == Side.Player);
            if (ArrowToEnemy != null) ArrowToEnemy.SetActive(s == Side.Enemy);
            TurnLabel.text = (s == Side.Player ? "YOUR TURN" : "ENEMY TURN") + "  #" + Engine.State.TurnNumber;
            if (s == Side.Player) AudioDirector.Sfx("turn");
            StartCoroutine(CombatantHud.Pop(TurnLabel.rectTransform, 1.25f));
        }

        private void RefreshActionBar()
        {
            if (Engine == null) return;
            var me = Engine.State.Get(Side.Player);
            bool myTurn = Engine.IsTurnOf(Side.Player) && !AutoPlay;
            for (int i = 0; i < CardButtons.Length; i++)
            {
                var b = CardButtons[i];
                if (i >= me.Cards.Count)
                {
                    b.Hide();
                    continue;
                }
                var c = me.Cards[i];
                string cost = c.Def.ManaCost > 0 ? c.Def.ManaCost + " MP" : "";
                if (c.Def.RageCost > 0) cost += (cost.Length > 0 ? " " : "") + c.Def.RageCost + " RG";
                if (cost.Length == 0) cost = "Free";
                b.Bind(c.Def.Name, cost, c.UsesLeft + "/" + c.Def.UsesPerBattle, sprites.Get(c.Def.IconKey), new Color(0.35f, 0.55f, 0.85f));
                b.SetUsable(myTurn && Engine.CardBlockReason(Side.Player, i) == null);
            }
            for (int i = 0; i < SkillButtons.Length; i++)
            {
                var b = SkillButtons[i];
                if (i >= me.Skills.Count)
                {
                    b.Hide();
                    continue;
                }
                var sk = me.Skills[i];
                string cost = sk.ManaCost + " MP" + (sk.RageCost > 0 ? " " + sk.RageCost + " RG" : "");
                b.Bind(sk.Name, cost, "SKILL", sprites.Get(sk.IconKey), new Color(0.9f, 0.6f, 0.2f));
                b.SetUsable(myTurn && Engine.SkillBlockReason(Side.Player, i) == null);
            }
        }

        private IEnumerator ShowBanner(string text, float hold)
        {
            if (Banner == null) yield break;
            BannerLabel.text = text;
            yield return Tween.Run(0.15f, t => Banner.alpha = t);
            yield return Tween.Wait(hold);
            yield return Tween.Run(0.15f, t => Banner.alpha = 1f - t);
        }

        private void Toast(string msg)
        {
            var app = GameApp.Instance;
            if (app != null) app.Toast(msg);
        }
    }
}
