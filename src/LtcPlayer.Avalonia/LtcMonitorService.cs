using LtcPlayer.App;
namespace LtcPlayer.Avalonia;
public sealed class LtcMonitorService : IAsyncDisposable
{
 CancellationTokenSource? cancel;Task? worker;
 public MasterClock Clock{get;private set;}=new(TimecodeRate.Modes[1]);
 public float Level{get;private set;}public string Status{get;private set;}="未接続";
 public int Starts{get;private set;}
 public async Task ConfigureAsync(InputSettings settings,Func<MasterClock,CancellationToken,Task>? source=null)
 {
  if(cancel!=null){cancel.Cancel();if(worker!=null)try{await worker;}catch(OperationCanceledException){}cancel.Dispose();}
  var mode=TimecodeRate.Modes.First(r=>r.Name==settings.Rate);Clock=new(mode,settings.Freewheel,settings.Resync);Level=0;Status="入力待機";cancel=new();Starts++;
  if(source!=null){var clock=Clock;var token=cancel.Token;worker=Task.Run(()=>source(clock,token));}
  else if(settings.DeviceId!="")worker=new AudioInputService().RunAsync(settings.DeviceId,settings.Channel,mode,Clock,(p,s)=>{Level=p;Status=s;},null,cancel.Token);
  else{Clock.Fault();Status="設定から入力機器を選択してください。";worker=Task.CompletedTask;}
 }
 public async ValueTask DisposeAsync(){cancel?.Cancel();if(worker!=null)try{await worker;}catch(OperationCanceledException){}cancel?.Dispose();cancel=null;}
}
