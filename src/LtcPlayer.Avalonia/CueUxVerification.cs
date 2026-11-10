using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
namespace LtcPlayer.Avalonia;
public static class CueUxVerification
{
 static void Check(bool value,string message){if(!value)throw new IOException(message);}
 public static async Task RunAsync(MainWindow w,string folder){
  Directory.CreateDirectory(folder);var results=new List<object>();bool failed=false;
  async Task Test(string name,Func<Task> body){try{await body();results.Add(new{test=name,status="PASS"});}catch(Exception e){failed=true;results.Add(new{test=name,status="FAIL",detail=e.ToString()});}File.WriteAllText(Path.Combine(folder,"results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));}
  try{
   await DefaultTestShow.LoadAsync(w.Editor);w.Width=1640;w.Height=940;if(Program.Arguments.Contains("--visual-cue-ux")||Program.Arguments.Contains("--visual-cue-insertion")){w.Topmost=true;var area=w.Screens.Primary!.WorkingArea;w.Position=new PixelPoint(area.X+40,area.Y+40);}await Task.Delay(300);var grid=w.FindControl<DataGrid>("CueGrid")!;
   await Test("play and stop vector icons centered",async()=>{await Task.Delay(100);foreach(var b in grid.GetVisualDescendants().OfType<Button>().Where(b=>b.Classes.Contains("cue-play")||b.Classes.Contains("cue-stop"))){Check(b.Content is PathIcon,"text glyph remains");var icon=(PathIcon)b.Content!;var point=icon.TranslatePoint(default,b)!.Value;Check(Math.Abs(point.X+icon.Bounds.Width/2-b.Bounds.Width/2)<1&&Math.Abs(point.Y+icon.Bounds.Height/2-b.Bounds.Height/2)<1,"icon center mismatch");}});
   await Test("TC fields and separators share font and line metrics",()=>{var texts=grid.GetVisualDescendants().OfType<TextBox>().Where(t=>t.Classes.Contains("timecode")).ToArray();Check(texts.Length>=16,"TC controls missing");foreach(var t in texts)Check(t.FontSize==14&&t.LineHeight==20&&t.VerticalContentAlignment==global::Avalonia.Layout.VerticalAlignment.Center,"TC alignment");foreach(var t in grid.GetVisualDescendants().OfType<TextBlock>().Where(t=>t.Classes.Contains("tc-separator")))Check(t.FontSize==14&&t.LineHeight==20&&t.FontFamily.ToString()=="Consolas","separator metrics");return Task.CompletedTask;});
   if(Program.Arguments.Contains("--visual-cue-ux")){w.Title="LTC Player — cue layout verification";await Task.Delay(30000);}
   await Test("insert boundaries convert correctly moving up and down",()=>{Check(MainWindow.InsertionMoveIndex(0,3)==2,"down");Check(MainWindow.InsertionMoveIndex(3,1)==1,"up");Check(MainWindow.InsertionMoveIndex(1,1)==1&&MainWindow.InsertionMoveIndex(1,2)==1,"adjacent no-op");Check(MainWindow.InsertionMoveIndex(2,0)==0&&MainWindow.InsertionMoveIndex(0,4)==3,"first/last");Check(MainWindow.InsertionMoveIndex(0,null)==-1,"outside");return Task.CompletedTask;});
   await Test("blue insertion line between rows; outside hides line",async()=>{var rows=grid.GetVisualDescendants().OfType<DataGridRow>().OrderBy(x=>x.TranslatePoint(default,grid)!.Value.Y).ToArray();var top=rows[2].TranslatePoint(default,grid)!.Value.Y;w.UpdateCueInsertion(new Point(100,top+2),false);await Task.Delay(80);var line=w.FindControl<Border>("CueInsertionLine")!;Check(line.IsVisible&&Math.Abs(Canvas.GetTop(line)-(top-2))<1,"wrong insertion boundary");if(Program.Arguments.Contains("--visual-cue-ux")||Program.Arguments.Contains("--visual-cue-insertion")){w.Title="LTC Player — cue insertion verification";await Task.Delay(30000);}w.UpdateCueInsertion(new Point(-5,top),false);Check(!line.IsVisible,"outside line retained");});
   await Test("reorder keeps cue IDs and scheduled timecodes",()=>{var row=w.Editor.Rows[0];string tc=row.StartTimecode;var id=row.CueId;w.Editor.Move(row,MainWindow.InsertionMoveIndex(0,3));Check(w.Editor.Rows[2].CueId==id&&row.StartTimecode==tc,"schedule changed");return Task.CompletedTask;});
  }finally{await w.Monitor.DisposeAsync();MainWindow.Shutdown(failed?1:0);}
 }
}
