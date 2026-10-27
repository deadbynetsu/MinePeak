package dev.skycraft.world;

public final class SkyTri {
   public static final double WALKABLE_NY = 0.7;
   public final double ax;
   public final double ay;
   public final double az;
   public final double bx;
   public final double by;
   public final double bz;
   public final double cx;
   public final double cy;
   public final double cz;
   public final double nx;
   public final double ny;
   public final double nz;
   public final double minX;
   public final double minY;
   public final double minZ;
   public final double maxX;
   public final double maxY;
   public final double maxZ;
   public final boolean stairHelper;
   public final boolean walkable;
   public final boolean diggable;
   public final int material;
   public final boolean terrain;

   public SkyTri(float[] var1, int var2, boolean var3) {
      this(var1, var2, var3 ? 1 : 0);
   }

   public SkyTri(float[] var1, int var2, int var3) {
      boolean var4 = (var3 & 1) != 0;
      this.diggable = (var3 & 2) != 0;
      this.material = var3 >>> 8 & 0xFF;
      this.terrain = (var3 & 8) != 0;
      this.ax = var1[var2];
      this.ay = var1[var2 + 1];
      this.az = var1[var2 + 2];
      this.bx = var1[var2 + 3];
      this.by = var1[var2 + 4];
      this.bz = var1[var2 + 5];
      this.cx = var1[var2 + 6];
      this.cy = var1[var2 + 7];
      this.cz = var1[var2 + 8];
      double var5 = this.bx - this.ax;
      double var7 = this.by - this.ay;
      double var9 = this.bz - this.az;
      double var11 = this.cx - this.ax;
      double var13 = this.cy - this.ay;
      double var15 = this.cz - this.az;
      double var17 = var7 * var15 - var9 * var13;
      double var19 = var9 * var11 - var5 * var15;
      double var21 = var5 * var13 - var7 * var11;
      double var23 = Math.sqrt(var17 * var17 + var19 * var19 + var21 * var21);
      if (var23 < 1.0E-12) {
         this.nx = 0.0;
         this.ny = 1.0;
         this.nz = 0.0;
      } else {
         this.nx = var17 / var23;
         this.ny = var19 / var23;
         this.nz = var21 / var23;
      }

      this.minX = Math.min(this.ax, Math.min(this.bx, this.cx));
      this.minY = Math.min(this.ay, Math.min(this.by, this.cy));
      this.minZ = Math.min(this.az, Math.min(this.bz, this.cz));
      this.maxX = Math.max(this.ax, Math.max(this.bx, this.cx));
      this.maxY = Math.max(this.ay, Math.max(this.by, this.cy));
      this.maxZ = Math.max(this.az, Math.max(this.bz, this.cz));
      this.stairHelper = var4;
      this.walkable = var4 || Math.abs(this.ny) >= 0.7;
   }

   public boolean degenerate() {
      return this.maxX - this.minX < 1.0E-9 && this.maxZ - this.minZ < 1.0E-9;
   }

   public double heightAt(double var1, double var3) {
      if (!(var1 < this.minX - 1.0E-9)
         && !(var1 > this.maxX + 1.0E-9)
         && !(var3 < this.minZ - 1.0E-9)
         && !(var3 > this.maxZ + 1.0E-9)
         && !(Math.abs(this.ny) < 0.05)) {
         double var5 = edge(var1, var3, this.ax, this.az, this.bx, this.bz);
         double var7 = edge(var1, var3, this.bx, this.bz, this.cx, this.cz);
         double var9 = edge(var1, var3, this.cx, this.cz, this.ax, this.az);
         boolean var11 = var5 < -1.0E-12 || var7 < -1.0E-12 || var9 < -1.0E-12;
         boolean var12 = var5 > 1.0E-12 || var7 > 1.0E-12 || var9 > 1.0E-12;
         return var11 && var12 ? Double.NaN : this.ay - (this.nx * (var1 - this.ax) + this.nz * (var3 - this.az)) / this.ny;
      } else {
         return Double.NaN;
      }
   }

   private static double edge(double var0, double var2, double var4, double var6, double var8, double var10) {
      return (var8 - var4) * (var2 - var6) - (var10 - var6) * (var0 - var4);
   }
}
