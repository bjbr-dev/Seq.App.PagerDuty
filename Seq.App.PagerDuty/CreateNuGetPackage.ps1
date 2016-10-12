nuget pack -build .\Seq.App.PagerDuty.csproj -Prop Configuration=Release -Prop Platform=AnyCPU

$x = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")