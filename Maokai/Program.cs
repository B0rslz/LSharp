#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace Maokai
{
    internal class Program
    {
        public const string ChampionName = "Maokai";

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        private static SpellSlot IgniteSlot;
        private static SpellSlot SmiteSlot;

        public static Menu Config;

        private static Obj_AI_Hero Player;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.BaseSkinName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 525);
            E = new Spell(SpellSlot.E, 1100);
            R = new Spell(SpellSlot.R, 500);

            // To-DO : Exclusive smites S5
            IgniteSlot = Player.GetSpellSlot("SummonerDot");
            SmiteSlot = Player.GetSpellSlot("SummonerSmite");

            Q.SetSkillshot(0.3333f, 110, 1100, false, SkillshotType.SkillshotLine);
            W.SetTargetted(0.5f, 1000);
            E.SetSkillshot(0.25f, 225, 1750, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 478, float.MaxValue, false, SkillshotType.SkillshotCircle);

            SpellList.AddRange(new[] { Q, W, E, R });

            Config = new Menu(ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("RUsage", "R Usage"));
            Config.SubMenu("RUsage").AddItem(new MenuItem("UseR", "Use R").SetValue(true));
            Config.SubMenu("RUsage").AddItem(new MenuItem("MinR", "Enemies to use R").SetValue<Slider>(new Slider(3, 1, 5)));

            Config.AddSubMenu(new Menu("ManaManager", "Mana Manager"));
            Config.SubMenu("ManaManager").AddItem(new MenuItem("ManaHarass", "Don't harass if mana %").SetValue(new Slider(40, 100, 0)));
            Config.SubMenu("ManaManager").AddItem(new MenuItem("ManaR", "% Mana to turn off R").SetValue(new Slider(30, 100, 0)));
            Config.SubMenu("ManaManager").AddItem(new MenuItem("ManaFarm", "Min Mana to Farm").SetValue(new Slider(60, 100, 0)));

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm").AddItem(new MenuItem("waveNumQ", "Minions to hit with Q").SetValue<Slider>(new Slider(3, 1, 10)));
            Config.SubMenu("Farm").AddItem(new MenuItem("waveNumE", "Minions to hit with E").SetValue<Slider>(new Slider(4, 1, 10)));

            // KillSteal Option: TO-DO
            /* Config.AddSubMenu(new Menu("KSMenu", "KS Menu"));
            Config.SubMenu("KSMenu").AddItem(new MenuItem("QKS", "Q to KS").SetValue(true);
            */

            Config.AddSubMenu(new Menu("Keys", "Keys"));
            Config.SubMenu("Keys").AddItem(new MenuItem("ComboActive", "Combo").SetValue(new KeyBind(32, KeyBindType.Press)));
            Config.SubMenu("Keys").AddItem(new MenuItem("GanksActive", "Ganks").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Keys").AddItem(new MenuItem("HarassActive", "Harass").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Keys").AddItem(new MenuItem("LaneClearActive", "LaneClear").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Keys").AddItem(new MenuItem("JungleFarmActive", "JungleFarm").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Keys").AddItem(new MenuItem("AutoSmite", "Auto Smite").SetValue<KeyBind>(new KeyBind('J', KeyBindType.Toggle)));
            Config.SubMenu("Keys").AddItem(new MenuItem("harassToggle", "Use Harass (toggle)").SetValue<KeyBind>(new KeyBind('T', KeyBindType.Toggle)));

            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Draw damage after a rotation").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit += hero => (float)(ObjectManager.Player.GetSpellDamage(hero, SpellSlot.Q) + ObjectManager.Player.GetSpellDamage(hero, SpellSlot.W) + ObjectManager.Player.GetSpellDamage(hero, SpellSlot.E) + ObjectManager.Player.GetSpellDamage(hero, SpellSlot.R));
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate (object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(false, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));
            Config.SubMenu("Drawings").AddItem(dmgAfterComboItem);
            Config.AddToMainMenu();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;

            Game.PrintChat("<font color=\"#008B8B\">Maokai by B0rslz</font>");
            Game.PrintChat("If you like this one, leave a feedback infórum");

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active)
                {
                    Utility.DrawCircle(Player.Position, spell.Range, menuItem.Color);
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
                    Harass();
                AutoIgnite();

                if (Config.Item("GanksActive").GetValue<KeyBind>().Active)
                    Ganks();
                AutoIgnite();

                if (Config.Item("harassToggle").GetValue<KeyBind>().Active)
                    ToggleHarass();
                AutoIgnite();

                if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
                    LaneClear();
                AutoIgnite();

                if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
                    JungleFarm();
                AutoIgnite();

                if (Config.Item("AutoW").GetValue<bool>())
                    AutoUnderTower();
                AutoIgnite();

                if (Config.Item("AutoSmite").GetValue<KeyBind>().Active)
                    AutoSmite();
                AutoIgnite();

            }
        }

        private static void AutoIgnite()
        {
            var iTarget = TargetSelector.GetTarget(600, TargetSelector.DamageType.True);
            var Idamage = ObjectManager.Player.GetSummonerSpellDamage(iTarget, Damage.SummonerSpell.Ignite) * 0.90;

            if (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready && iTarget.Health < Idamage)
            {
                Player.Spellbook.CastSpell(IgniteSlot, iTarget);
            }
        }

        private static void AutoSmite()
        {
            if (Config.Item("AutoSmite").GetValue<KeyBind>().Active)
            {
                float[] SmiteDmg = { 20 * Player.Level + 370, 30 * Player.Level + 330, 40 * Player.Level + 240, 50 * Player.Level + 100 };
                string[] MonsterNames = { "LizardElder", "AncientGolem", "Worm", "Dragon" };
                var vMinions = MinionManager.GetMinions(Player.ServerPosition, Player.Spellbook.Spells.FirstOrDefault(
                    spell => spell.Name.Contains("smite")).SData.CastRange[0], MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.Health);
                foreach (var vMinion in vMinions)
                {
                    if (vMinion != null
                        && !vMinion.IsDead
                        && !Player.IsDead
                        && !Player.IsStunned
                        && SmiteSlot != SpellSlot.Unknown
                        && Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready)
                    {
                        if ((vMinion.Health < SmiteDmg.Max()) && (MonsterNames.Any(name => vMinion.BaseSkinName.StartsWith(name))))
                        {
                            Player.Spellbook.CastSpell(SmiteSlot, vMinion);

                        }
                    }
                }
            }
        }

        private static void AutoUlt()
        {
            int inimigos = Utility.CountEnemysInRange(650);

            var RMana = Config.Item("ManaR").GetValue<Slider>().Value;
            var MPercentR = Player.Mana * 100 / Player.MaxMana;

            if (Config.Item("MinR").GetValue<Slider>().Value <= inimigos && MPercentR >= RMana)
            {
                R.Cast();
            }

        }

        private static void AutoUnderTower()
        {
            var wTarget = TargetSelector.GetTarget(W.Range + W.Width, TargetSelector.DamageType.Magical);

            if (Utility.UnderTurret(wTarget, false) && W.IsReady())
            {
                W.Cast(wTarget);
            }
        }

        private static void Combo()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range + Q.Width, TargetSelector.DamageType.Magical);
            var wTarget = TargetSelector.GetTarget(W.Range + W.Width, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range + E.Width, TargetSelector.DamageType.Magical);

            if (wTarget != null && W.IsReady())
            {
                W.Cast(wTarget);
            }
            if (qTarget != null && Q.IsReady())
            {
                if (qTarget.IsVisible)
                    Q.Cast(qTarget);
            }
            if (eTarget != null && E.IsReady())
            {
                E.Cast(eTarget);
            }
            if (Config.Item("UseR").GetValue<bool>() && R.IsReady())
            {
                AutoUlt();
            }
        }

        private static void Ganks()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range + Q.Width, TargetSelector.DamageType.Magical);
            var wTarget = TargetSelector.GetTarget(W.Range + W.Width, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range + E.Width, TargetSelector.DamageType.Magical);

            if (qTarget != null && Q.IsReady())
            {
                Q.Cast(qTarget);
            }
            if (wTarget != null && W.IsReady())
            {
                W.Cast(wTarget);
            }
            if (eTarget != null && E.IsReady())
            {
                E.Cast(eTarget);
            }
        }

        private static void Harass()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range + Q.Width, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range + E.Width, TargetSelector.DamageType.Magical);

            if (qTarget != null && Q.IsReady())
            {
                if (qTarget.IsVisible)
                    Q.Cast(qTarget);
            }
            if (eTarget != null && E.IsReady())
            {
                E.Cast(eTarget);
            }
        }

        private static void ToggleHarass()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range + Q.Width, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range + E.Width, TargetSelector.DamageType.Magical);

            if (qTarget != null && Q.IsReady())
            {
                if (qTarget.IsVisible)
                    Q.Cast(qTarget);
            }
            if (eTarget != null && E.IsReady())
            {
                E.Cast(eTarget);
            }
        }

        private static void LaneClear()
        {
            var allMinionsQ = MinionManager.GetMinions(Player.ServerPosition, Q.Range + Q.Width + 30, MinionTypes.All);
            var allMinionsE = MinionManager.GetMinions(Player.ServerPosition, E.Range + E.Width + 30, MinionTypes.All);

            var FMana = Config.Item("ManaFarm").GetValue<Slider>().Value;
            var MPercent = Player.Mana * 100 / Player.MaxMana;

            var fle = E.GetCircularFarmLocation(allMinionsE, E.Width);
            var flq = Q.GetLineFarmLocation(allMinionsQ, Q.Width);

            if (Q.IsReady() && flq.MinionsHit >= Config.Item("waveNumQ").GetValue<Slider>().Value && flq.MinionsHit >= 2 && MPercent >= FMana)
            {
                Q.Cast(flq.Position);
            }
            if (E.IsReady() && fle.MinionsHit >= Config.Item("waveNumE").GetValue<Slider>().Value && fle.MinionsHit >= 3 && MPercent >= FMana)
            {
                E.Cast(fle.Position);
            }
        }

        private static void JungleFarm()
        {
            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                W.Cast(mob);
                Q.Cast(mob);
                E.Cast(mob);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (gapcloser.Sender.IsValidTarget(400f))
            {
                Q.Cast(gapcloser.Sender);
            }
        }

        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (unit.IsValidTarget(600f) && (spell.DangerLevel != InterruptableDangerLevel.Low) && Q.IsReady())
            {
                Q.Cast(unit);
                if (Q.IsReady() && unit.IsValidTarget(W.Range))
                {
                    W.Cast(unit);
                }
            }
        }
    }
}
