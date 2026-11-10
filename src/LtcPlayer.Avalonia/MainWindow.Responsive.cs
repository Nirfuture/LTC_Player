using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
namespace LtcPlayer.Avalonia;
public partial class MainWindow
{
 bool updatingLayout;
 internal bool CompactLayout{get;private set;}
 void InitializeResponsiveLayout(){
  WindowPlacement.Track(this);
  SizeChanged+=(_,_)=>UpdateResponsiveLayout();
  Control<ScrollViewer>("WorkspaceScroll").SizeChanged+=(_,_)=>UpdateResponsiveLayout();
  Control<Grid>("PreviewPane").SizeChanged+=(_,_)=>UpdateResponsiveLayout();
  Opened+=(_,_)=>UpdateResponsiveLayout();
 }
 internal void UpdateResponsiveLayout(){
  if(updatingLayout)return;updatingLayout=true;
  try{
   bool compact=Bounds.Width<1180||Bounds.Height<650;
   var body=Control<Grid>("Workspace");var scroll=Control<ScrollViewer>("WorkspaceScroll");var editor=Control<Border>("EditorPane");var preview=Control<Grid>("PreviewPane");var viewport=Control<Grid>("PreviewViewport");
   Control<Grid>("MainLayout").Margin=new Thickness(compact?12:22);
   if(compact!=CompactLayout||body.RowDefinitions.Count==0){
    CompactLayout=compact;
    body.ColumnDefinitions=new ColumnDefinitions(compact?"*":"*,18,420");
    body.RowDefinitions=new RowDefinitions(compact?"Auto,14,Auto":"*");
    global::Avalonia.Controls.Grid.SetColumn(preview,compact?0:2);global::Avalonia.Controls.Grid.SetRow(preview,compact?2:0);
    scroll.VerticalScrollBarVisibility=ScrollBarVisibility.Auto;
    preview.RowDefinitions=new RowDefinitions(compact?"Auto,Auto,Auto,Auto,Auto,Auto,Auto":"Auto,*,Auto,Auto,Auto,Auto,Auto");
   }
   if(compact){
    body.Height=double.NaN;
    double available=Math.Max(300,scroll.Bounds.Height);
    editor.Height=Math.Clamp(available*.36,available>=750?340:220,420);
    double chrome=preview.Bounds.Height>0?Math.Max(260,preview.Bounds.Height-viewport.Bounds.Height):320;
    viewport.Height=Math.Clamp(available-editor.Height-14-chrome,160,520);
   }else{body.Height=Math.Max(520,scroll.Bounds.Height);editor.Height=double.NaN;viewport.Height=double.NaN;}
   UpdatePreviewGeometry();
  }finally{updatingLayout=false;}
 }
}
