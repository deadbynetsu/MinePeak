using System;
using System.Buffers.Binary;
using PeakCraft.World;
static class Program {
 static int Main() {
  Check("tall vertical wall spans all regions", -3, 19, -10, 10, true, -1, 2);
  Check("negative Minecraft heights floor correctly", -511.9f, -503.1f, -70, -50, true, -64, -63);
  Check("flat face on boundary reaches both sides", -512, -512, -65, -63, true, -65, -64);
  Check("window clips the range", -700, 0, -64, -61, true, -64, -61);
  Check("out of window is absent, not clamped", 80, 90, -64, -60, false, 0, 0);
  Check("invalid mesh vertex is rejected", float.NaN, 0, -64, -60, false, 0, 0);
  BlockPackets();
  Console.WriteLine("PASS: six collision region and twelve native block packet regression cases"); return 0;
 }
 static void Check(string name,float lo,float hi,int wlo,int whi,bool expected,int first,int last) {
  bool actual=CollisionRegionSpan.TryGet(lo,hi,wlo,whi,out int a,out int b);
  if(actual!=expected || actual&&(a!=first||b!=last)) throw new Exception(name+": "+actual+" "+a+".."+b);
  Console.WriteLine("PASS: "+name);
 }
 static void BlockPackets() {
  var full = Packet(7, -32, -9, 5, 13, new[]{new BlockCollisionBox(1,2,3,2,3,4)});
  Accept("exact little endian block box and negative section coordinates", full, 5,13,true,1);
  BlockCollisionPacket.TryRead(full,5,13,out int x,out int y,out int z,out var boxes);
  if(x!=7||y!=-32||z!=-9||boxes[0].MinX!=1||boxes[0].MaxZ!=4) throw new Exception("wire coordinates changed");
  Accept("empty section decodes as a support-clear packet",Packet(7,-32,-9,5,13,Array.Empty<BlockCollisionBox>()),5,13,true,0);
  Accept("slab keeps half height instead of a full cube",Packet(0,0,0,5,13,new[]{new BlockCollisionBox(0,0,0,1,.5f,1)}),5,13,true,1,.5f);
  Accept("fence can extend above the section",Packet(0,0,0,5,13,new[]{new BlockCollisionBox(.375f,15,.375f,.625f,16.5f,.625f)}),5,13,true,1,16.5f);
  Accept("old scene packet cannot create ghost support",full,6,13,false,0);
  Accept("old collision epoch cannot restore cleared support",full,5,14,false,0);
  Accept("truncated box packet is rejected",full.AsSpan(0,full.Length-1).ToArray(),5,13,false,0);
  var count = (byte[])full.Clone(); BinaryPrimitives.WriteInt32LittleEndian(count.AsSpan(12),int.MaxValue);
  Accept("unbounded box count is rejected before allocation",count,5,13,false,0);
  Accept("nonfinite shape is rejected",Packet(0,0,0,5,13,new[]{new BlockCollisionBox(float.NaN,0,0,1,1,1)}),5,13,false,0);
  Accept("inverted box cannot build a collider",Packet(0,0,0,5,13,new[]{new BlockCollisionBox(1,0,0,0,1,1)}),5,13,false,0);
  Accept("zero volume box is rejected",Packet(0,0,0,5,13,new[]{new BlockCollisionBox(0,0,0,1,0,1)}),5,13,false,0);
  Accept("out of section arbitrary geometry is rejected",Packet(0,0,0,5,13,new[]{new BlockCollisionBox(0,0,0,1000,1,1)}),5,13,false,0);
 }
 static byte[] Packet(int sx,int sy,int sz,uint world,uint epoch,BlockCollisionBox[] boxes) {
  var data=new byte[24+boxes.Length*24];
  void I(int offset,int value)=>BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(offset,4),value);
  void F(int offset,float value)=>I(offset,BitConverter.SingleToInt32Bits(value));
  I(0,sx);I(4,sy);I(8,sz);I(12,boxes.Length);I(16,(int)world);I(20,(int)epoch);
  for(int i=0;i<boxes.Length;i++) {var b=boxes[i];int p=24+i*24;F(p,b.MinX);F(p+4,b.MinY);F(p+8,b.MinZ);F(p+12,b.MaxX);F(p+16,b.MaxY);F(p+20,b.MaxZ);}
  return data;
 }
 static void Accept(string name,byte[] data,uint world,uint epoch,bool accepted,int count,float? maxY=null) {
  bool actual=BlockCollisionPacket.TryRead(data,world,epoch,out _,out _,out _,out var boxes);
  if(actual!=accepted || actual&&boxes.Length!=count) throw new Exception(name+": incorrect decode result");
  if(actual && maxY.HasValue && (boxes.Length==0 || boxes[0].MaxY!=maxY.Value)) throw new Exception(name+": collision height changed");
  Console.WriteLine("PASS: "+name);
 }
}
