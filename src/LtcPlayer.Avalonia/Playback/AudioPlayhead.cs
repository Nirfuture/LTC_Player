namespace LtcPlayer.App;

// Produced by the active playback source. Audio never schedules a cue independently.
public sealed record AudioPlayhead(Guid SessionId,Guid CueId,int Generation,string Path,double Position,double Duration,bool Advancing,double HostSeconds,bool LtcDriven=false,string Notice="")
{
    public static AudioPlayhead Empty{get;}=new(Guid.Empty,Guid.Empty,0,"",0,0,false,0);
    public double Estimate(double now)=>Math.Clamp(Position+(Advancing?Math.Max(0,now-HostSeconds):0),0,Math.Max(0,Duration));
    public bool Usable(double now)=>SessionId!=Guid.Empty&&CueId!=Guid.Empty&&Path!=""&&Duration>0&&Advancing&&now-HostSeconds<.250;
}
