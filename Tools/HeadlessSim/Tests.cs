using System;
using Riftbound;
using Riftbound.Config;
using Riftbound.Simulation;
using UnityEngine;

static class Tests {
  static int fails;
  static void Check(bool c, string msg){ Console.WriteLine((c?"PASS ":"FAIL ")+msg); if(!c) fails++; }
  static CardDefinition Card(GameConfig cfg, string id){ foreach(var c in cfg.deck) if(c.cardId==id) return c; return null; }
  static int Slot(Match m, Team t, string id){ return m.GetDeck(t).FindSlot(id); }
  static void EnsureInHand(Match m, Team t, string id){ int guard=0; while(Slot(m,t,id)<0 && guard++<20) m.GetDeck(t).Consume(0); }
  public static int Run() {
    var cfg = DefaultContent.CreateConfig(); cfg.riftShiftEnabled=false;
    // 1. Deploy rules
    var m = new Match(cfg, 1);
    Check(m.Cards.IsValidDeploy(Team.Player, new Vector2(0,-3)), "player can deploy in own half");
    Check(!m.Cards.IsValidDeploy(Team.Player, new Vector2(0,3)), "player cannot deploy in enemy half");
    Check(!m.Cards.IsValidDeploy(Team.Player, new Vector2(0,0)), "player cannot deploy at neutral centre");
    m.GetRift(NodeId.RiftC).ForceOwner(Team.Player);
    Check(m.Cards.IsValidDeploy(Team.Player, new Vector2(0,0.5f)), "player can deploy near owned Rift C");
    Check(!m.Cards.IsValidDeploy(Team.Player, new Vector2(0,-5)), "cannot deploy inside own core");
    // 2. Flux & play
    m = new Match(cfg, 2);
    EnsureInHand(m, Team.Player, "MAW");
    var r = m.Submit(new PlayCardCommand(Team.Player, Slot(m,Team.Player,"MAW"), new Vector2(0,-3)));
    Check(r==PlayResult.Ok && m.Units.Count(Team.Player)==1, "MAW played with 5 flux");
    Check(m.GetFlux(Team.Player).Current < 0.01f, "flux spent");
    EnsureInHand(m, Team.Player, "BLINK");
    r = m.Submit(new PlayCardCommand(Team.Player, Slot(m,Team.Player,"BLINK"), new Vector2(0,-3)));
    Check(r==PlayResult.NotEnoughFlux, "cannot play without flux");
    for(int i=0;i<60;i++) m.Tick(1f/30f);
    Check(Math.Abs(m.GetFlux(Team.Player).Current-2f)<0.05f, "flux regen 1/s ("+m.GetFlux(Team.Player).Current+")");
    // 3. Capture by one unit takes captureSeconds
    m = new Match(cfg, 3);
    m.SpawnUnit(Card(cfg,"MAW"), Team.Player, m.GetRift(NodeId.RiftC).Position);
    float t=0; while(m.GetRift(NodeId.RiftC).Owner!=RiftOwner.Player && t<20){ m.Tick(0.05f); t+=0.05f; }
    Check(Math.Abs(t-cfg.captureSeconds)<0.3f, "one unit captures in ~captureSeconds ("+t.ToString("0.00")+")");
    // contested freeze
    m.SpawnUnit(Card(cfg,"ANCHOR"), Team.Enemy, m.GetRift(NodeId.RiftC).Position+new Vector2(0.3f,0));
    m.Tick(0.05f); m.Tick(0.05f);
    var rc=m.GetRift(NodeId.RiftC); float before=rc.Control; m.Tick(0.05f);
    Check(rc.Contested && Math.Abs(rc.Control-before)<1e-4, "contested rift freezes");
    // 4. Rift break -> core vulnerable, rifts reset, damage, end
    m = new Match(cfg, 4);
    foreach (var rr in m.Rifts) rr.ForceOwner(Team.Player);
    m.Tick(0.02f);
    Check(m.RiftBreak.IsActiveFor(Team.Player) && m.GetCore(Team.Enemy).Vulnerable, "owning 3 rifts triggers RIFT BREAK");
    Check(m.OwnedRiftCount(Team.Player)==0, "rifts reset to neutral on break");
    for (int i=0;i<(int)(cfg.riftBreakDuration/0.05f)+2;i++) m.Tick(0.05f);
    Check(!m.RiftBreak.Active && !m.GetCore(Team.Enemy).Vulnerable, "break ends after duration");
    m.DamageCore(Team.Player, 10, null);
    Check(Math.Abs(m.GetCore(Team.Enemy).Stability-100)<0.01, "core immune outside rift break");
    // units assault core during break
    m = new Match(cfg, 5);
    for(int i=0;i<3;i++) m.SpawnUnit(Card(cfg,"HUNTER"), Team.Player, new Vector2(i-1,2.5f));
    m.RiftBreak.Begin(Team.Player);
    for (int i=0;i<(int)(8/0.05f);i++) m.Tick(0.05f);
    Check(m.GetCore(Team.Enemy).Stability < 100, "units damage vulnerable core ("+m.GetCore(Team.Enemy).Stability.ToString("0.0")+")");
    // core destroyed -> victory
    m = new Match(cfg, 6);
    m.DebugDamageCore(Team.Enemy, 100);
    Check(!m.IsRunning && m.HasWinner && m.Winner==Team.Player, "enemy core 0 => player wins");
    // 5. Time up
    var c2 = DefaultContent.CreateConfig(); c2.matchDuration=1f; c2.riftShiftEnabled=false;
    m = new Match(c2, 7); m.DebugDamageCore(Team.Player, 5);
    for(int i=0;i<30;i++) m.Tick(0.05f);
    Check(!m.IsRunning && m.Winner==Team.Enemy, "time up => higher stability wins: "+m.EndReason);
    m = new Match(c2, 8);
    for(int i=0;i<30;i++) m.Tick(0.05f);
    Check(m.Phase==MatchPhase.SuddenRift, "tie => SUDDEN RIFT");
    Check(Math.Abs(m.GetFlux(Team.Player).RegenMultiplier - c2.suddenFluxRegenMultiplier)<0.01, "sudden flux multiplier");
    m.RiftBreak.Begin(Team.Enemy); m.DamageCore(Team.Enemy, 0.5f, null);
    Check(!m.IsRunning && m.Winner==Team.Enemy, "sudden: first core damage wins");
    // 6. Pulse
    m = new Match(cfg, 9);
    var sw1=m.SpawnUnit(Card(cfg,"SWARM"), Team.Enemy, m.GetRift(NodeId.RiftC).Position);
    var mw=m.SpawnUnit(Card(cfg,"MAW"), Team.Enemy, m.GetRift(NodeId.RiftC).Position+new Vector2(0.5f,0));
    m.GetFlux(Team.Player).Add(10);
    EnsureInHand(m, Team.Player, "PULSE");
    r = m.Submit(new PlayCardCommand(Team.Player, Slot(m,Team.Player,"PULSE"), m.GetRift(NodeId.RiftC).Position));
    m.Tick(0.01f);
    Check(r==PlayResult.Ok && !sw1.Alive && mw.Alive && mw.Hp < mw.Stats.maxHp, "PULSE kills swarm, damages maw");
    r = m.Submit(new PlayCardCommand(Team.Player, 0, new Vector2(3.5f,-5f)));
    // 7. Parasite
    m = new Match(cfg, 10);
    var hunter=m.SpawnUnit(Card(cfg,"HUNTER"), Team.Enemy, new Vector2(3.5f,4.5f));
    m.GetFlux(Team.Player).Add(10);
    EnsureInHand(m, Team.Player, "PARASITE");
    r = m.Submit(new PlayCardCommand(Team.Player, Slot(m,Team.Player,"PARASITE"), hunter.Position+new Vector2(0.3f,0)));
    Check(r==PlayResult.Ok && hunter.Infected, "PARASITE infects selected enemy");
    int before2 = m.Units.Count(Team.Player);
    for(int i=0;i<(int)(10.2f/0.05f);i++) m.Tick(0.05f);
    Check(!hunter.Alive && m.Units.Count(Team.Player)==before2+2, "PARASITE kills host and spawns 2 allies");
    // 8. Leech drain
    m = new Match(cfg, 11);
    m.GetRift(NodeId.RiftB).ForceOwner(Team.Enemy);
    m.SpawnUnit(Card(cfg,"LEECH"), Team.Player, m.GetRift(NodeId.RiftB).Position);
    m.GetFlux(Team.Enemy).RegenPerSecond=0; m.GetFlux(Team.Enemy).Current=5;
    for(int i=0;i<20;i++) m.Tick(0.05f);
    Check(m.GetFlux(Team.Enemy).Current < 5f-0.2f, "LEECH drains enemy flux ("+m.GetFlux(Team.Enemy).Current.ToString("0.00")+")");
    // 9. Anchor reduction
    m = new Match(cfg, 12);
    var an = m.SpawnUnit(Card(cfg,"ANCHOR"), Team.Player, m.GetRift(NodeId.RiftA).Position);
    m.Tick(0.01f);
    m.ApplyRawDamage(an, 100, Team.Enemy);
    Check(Math.Abs(an.Hp-(an.Stats.maxHp-50))<0.01f, "ANCHOR takes 50% in rift");
    // 10. Deck rotation
    m = new Match(cfg, 13);
    var d=m.GetDeck(Team.Player); var first=d.GetSlot(0); var next=d.Next; d.Consume(0);
    Check(d.GetSlot(0)==next, "played slot is refilled with NEXT");
    for(int i=0;i<4;i++) d.Consume(0);
    Check(d.GetSlot(0)==first, "played card returns after the queue rotates");
    // 11. Shift keeps network connected
    var c3 = DefaultContent.CreateConfig(); m = new Match(c3, 14);
    bool ok=true; for(int i=0;i<200;i++){ m.RiftShift.Shift(); for(int a=0;a<5;a++) for(int b=0;b<5;b++) if(float.IsInfinity(m.Network.PathDistance((NodeId)a,(NodeId)b))) ok=false; }
    Check(ok, "every Rift Shift keeps all nodes connected");
    Console.WriteLine(fails==0? "ALL TESTS PASSED" : fails+" FAILURES");
    return fails;
  }
}
