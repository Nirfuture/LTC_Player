using System.Text.Json;
namespace LtcPlayer.App;
public record InputSettings(string DeviceId="",string DeviceName="",int Channel=0,string Rate="25",string Start="00:00:00:00",int Offset=0,double Freewheel=0.5,double Resync=2)
{
    public static string PathName=>Path.Combine(AppContext.BaseDirectory,"phase2-settings.json");
    public static InputSettings Load(string? path=null)
    {
        path??=PathName;
        if(!File.Exists(path)) return new();
        return JsonSerializer.Deserialize<InputSettings>(File.ReadAllText(path))??new();
    }
    public void Save(string? path=null)
    {
        path??=PathName;
        var temp=path+".tmp"; File.WriteAllText(temp,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));
        File.Move(temp,path,true);
    }
}
