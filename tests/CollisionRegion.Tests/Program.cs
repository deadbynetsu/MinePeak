using System;
using PeakCraft.World;
static class Program {
 static int Main() {
  Check("tall vertical wall spans all regions", -3, 19, -10, 10, true, -1, 2);
  Check("negative Minecraft heights floor correctly", -511.9f, -503.1f, -70, -50, true, -64, -63);
  Check("flat face on boundary reaches both sides", -512, -512, -65, -63, true, -65, -64);
  Check("window clips the range", -700, 0, -64, -61, true, -64, -61);
  Check("out of window is absent, not clamped", 80, 90, -64, -60, false, 0, 0);
  Check("invalid mesh vertex is rejected", float.NaN, 0, -64, -60, false, 0, 0);
  Console.WriteLine("PASS: six collision region regression cases"); return 0;
 }
 static void Check(string name,float lo,float hi,int wlo,int whi,bool expected,int first,int last) {
  bool actual=CollisionRegionSpan.TryGet(lo,hi,wlo,whi,out int a,out int b);
  if(actual!=expected || actual&&(a!=first||b!=last)) throw new Exception(name+": "+actual+" "+a+".."+b);
  Console.WriteLine("PASS: "+name);
 }
}
