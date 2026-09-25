using System;
using System.Linq;
using System.Collections.Generic;
using Riftbound;
using Riftbound.AI;
using Riftbound.Config;
using Riftbound.Simulation;

/// Headless balance simulator: runs the real Riftbound simulation code (Assets/Riftbound/Scripts/Simulation,
/// Config, AI) outside Unity, with a tiny UnityEngine stub (Stub.cs).
///   dotnet run --project Tools/HeadlessSim              -> bot-vs-bot balance report (all difficulties)
///   dotnet run --project Tools/HeadlessSim -- test      -> rule scenario tests (exit code = failures)
///   dotnet run --project Tools/HeadlessSim -- fair [seed] -> 400 Normal-vs-Normal matches (side fairness)
///   dotnet run --project Tools/HeadlessSim -- sweep     -> example parameter sweep (capture time x core damage)
///   dotnet run --project Tools/HeadlessSim -- dump      -> DefaultContent cards as JSON (for generate_unity_assets.py)
static class Program {
  static int seedBase=1000;
  static void Main(string[] args) {
    if (args.Length>1) seedBase=int.Parse(args[1]);
    var cfg = DefaultContent.CreateConfig();
    if (args.Length > 0 && args[0]=="test") { Environment.Exit(Tests.Run()); }
    if (args.Length > 0 && args[0]=="dump") { Dump.Run(); return; }
    if (args.Length > 0 && args[0]=="sweep") {
      foreach (var cap in new[]{5f,6f}) foreach (var sc in new[]{1f,0.7f,0.55f}) {
        var c = DefaultContent.CreateConfig(); c.captureSeconds = cap;
        foreach (var card in c.deck) card.unit.coreDamage *= sc;
        c.deck[7].parasiteSpawnCard.unit.coreDamage *= sc;
        Console.WriteLine($"#### capture {cap} coreScale {sc}");
        Run(c, BotDifficulty.Normal, BotDifficulty.Normal, 60, true);
        Run(c, BotDifficulty.Easy, BotDifficulty.Easy, 40, true);
      }
      return;
    }
    Run(cfg, BotDifficulty.Normal, BotDifficulty.Normal, 400, true);
    if (args.Length>0 && args[0]=="fair") return;
    Run(cfg, BotDifficulty.Hard, BotDifficulty.Normal, 200, true);
    Run(cfg, BotDifficulty.Normal, BotDifficulty.Easy, 200, true);
    Run(cfg, BotDifficulty.Hard, BotDifficulty.Easy, 100, true);
    Run(cfg, BotDifficulty.Easy, BotDifficulty.Easy, 100, true);
    Run(cfg, BotDifficulty.Normal, BotDifficulty.Normal, 40, false);
  }
  static void Run(GameConfig cfg, BotDifficulty pd, BotDifficulty ed, int n, bool playerActive) {
    int pw=0, ew=0, draws=0; var reasons=new Dictionary<string,int>(); double time=0; int breaksP=0, breaksE=0, sudden=0, maxUnits=0; int shifts=0; double coreDmg=0; double cards=0; float firstBreak=0; int firstBreakN=0;
    for (int i=0;i<n;i++) {
      var m = new Match(cfg, seedBase+i);
      var pb = new BotController(m, Team.Player, pd, 5000+i);
      var eb = new BotController(m, Team.Enemy, ed, 9000+i);
      bool sawSudden=false; float fb=-1;
      m.RiftBreak.Started += t => { if (fb<0) fb=m.Elapsed; };
      float dt=1f/30f; int steps=0;
      while (m.IsRunning && steps < 30*60*6) {
        if (playerActive) pb.Tick(dt);
        eb.Tick(dt); m.Tick(dt); steps++;
        if (m.Phase==MatchPhase.SuddenRift) sawSudden=true;
        maxUnits=Math.Max(maxUnits, m.Units.All.Count);
      }
      if (fb>=0){firstBreak+=fb;firstBreakN++;}
      if (m.IsRunning) { Console.WriteLine("STUCK match"); continue; }
      if (!m.HasWinner) draws++; else if (m.Winner==Team.Player) pw++; else ew++;
      reasons[m.EndReason]=reasons.GetValueOrDefault(m.EndReason)+1;
      time+=m.Elapsed; breaksP+=m.RiftBreak.BreakCount[0]; breaksE+=m.RiftBreak.BreakCount[1]; if(sawSudden) sudden++;
      shifts+=m.RiftShift.ShiftCount; coreDmg+=m.Stats.CoreDamageDealt[0]+m.Stats.CoreDamageDealt[1]; cards+=m.Stats.CardsPlayed[0]+m.Stats.CardsPlayed[1];
    }
    Console.WriteLine($"== {(playerActive?pd.ToString():"IDLE")} (P) vs {ed} (E), n={n}: P {pw} / E {ew} / draw {draws}; avg time {time/n:0}s; breaks/match P {breaksP/(double)n:0.00} E {breaksE/(double)n:0.00}; first break avg {(firstBreakN>0?firstBreak/firstBreakN:0):0}s ({firstBreakN} matches); sudden {sudden}; maxUnits {maxUnits}; cards/match {cards/n:0}; shifts {shifts/(double)n:0.0}; dmg/break {coreDmg/Math.Max(1,breaksP+breaksE):0.0}");
    foreach (var kv in reasons) Console.WriteLine($"     {kv.Key}: {kv.Value}");
  }
}
