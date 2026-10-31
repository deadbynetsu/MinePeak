import dev.skycraft.client.collision.WallPlaneAnchor;
import java.util.Locale;

/** Synthetic planes only: no native game geometry is included in these tests. */
public final class WallPlaneAnchorTests {
    private static int passed;
    private static final double[][] STEEP = {{-20, -29.2, -20}, {20, 30.8, 20}, {-20, 30.8, 20}};

    public static void main(String[] args) {
        test("old vertex-only side reverses when the same triangle starts at another vertex", () -> {
            Plane plane = new Plane(STEEP);
            check(Math.signum(plane.oldSide(0, 0, 0)) != Math.signum(plane.oldSide(1, 0, 0)));
        });
        test("a body above the steep plane is outside, independent of vertex order", () -> {
            Plane plane = new Plane(STEEP);
            for (int vertex=0; vertex<3; vertex++) check(plane.side(vertex, 0, 1.1, 0) < 0);
        });
        test("a body below the same plane keeps the opposite response", () -> {
            Plane plane = new Plane(STEEP);
            for (int vertex=0; vertex<3; vertex++) check(plane.side(vertex, 0, .4, 0) > 0);
        });
        test("all cyclic vertex orders produce the same signed plane distance", () -> {
            Plane plane = new Plane(STEEP);
            double expected = plane.side(0, .2, 1.1, .1);
            for (int vertex=1; vertex<3; vertex++) near(expected, plane.side(vertex, .2, 1.1, .1));
        });
        test("reversing winding preserves the oriented outward XZ response", () -> {
            Plane a = new Plane(STEEP);
            Plane b = new Plane(new double[][] {STEEP[2], STEEP[1], STEEP[0]});
            double signA = Math.signum(a.side(0, 0, 1.1, 0));
            double signB = Math.signum(b.side(0, 0, 1.1, 0));
            near(a.nx * signA, b.nx * signB); near(a.nz * signA, b.nz * signB);
        });
        test("Minecraft negative world height does not change the wall response", () -> translated(0, -512, 0));
        test("negative horizontal world regions do not change the wall response", () -> translated(-8192, -512, -8192));
        test("positive horizontal world regions do not change the wall response", () -> translated(8192, 1024, 8192));
        test("a quarter turn exchanges X/Z without changing side", () -> {
            double[][] rotated = new double[3][3];
            for (int i=0; i<3; i++) rotated[i] = new double[] {-STEEP[i][2], STEEP[i][1], STEEP[i][0]};
            Plane a = new Plane(STEEP), b = new Plane(rotated);
            near(a.side(0, .2, 1.1, .1), b.side(0, -.1, 1.1, .2));
        });
        test("vertical walls retain their original horizontal plane anchor", () -> {
            near(5, WallPlaneAnchor.coordinate(5, -400, 1, 0, 1, 100));
        });
        test("horizontal planes do not introduce division by zero", () -> {
            near(5, WallPlaneAnchor.coordinate(5, 8, 0, 1, 0, 9));
        });
        test("nearly horizontal excluded wall normals keep a finite anchor", () -> {
            near(5, WallPlaneAnchor.coordinate(5, 8, 1e-9, 1, 1e-18, 9));
        });
        test("the adjusted anchor lies on the original plane at the body height", () -> {
            Plane plane = new Plane(STEEP);
            for (int vertex=0; vertex<3; vertex++) {
                double[] v=plane.vertices[vertex];
                double x=WallPlaneAnchor.coordinate(v[0],v[1],plane.nx,plane.ny,plane.horizontal,1.1);
                double z=WallPlaneAnchor.coordinate(v[2],v[1],plane.nz,plane.ny,plane.horizontal,1.1);
                near(0,plane.nx*(x-v[0])+plane.ny*(1.1-v[1])+plane.nz*(z-v[2]));
            }
        });
        test("a vertex already at the body height remains unchanged", () -> {
            near(12, WallPlaneAnchor.coordinate(12, 4, .5, .5, .5, 4));
        });
        System.out.printf(Locale.ROOT, "PASS: %d sloped-wall plane tests%n", passed);
    }

    private static void translated(double x,double y,double z) {
        double[][] translated = new double[3][3];
        for(int i=0;i<3;i++) translated[i]=new double[] {STEEP[i][0]+x,STEEP[i][1]+y,STEEP[i][2]+z};
        Plane a=new Plane(STEEP),b=new Plane(translated);
        for(int i=0;i<3;i++) near(a.side(i,.2,1.1,.1),b.side(i,.2+x,1.1+y,.1+z));
    }

    private static final class Plane {
        final double[][] vertices; final double nx,ny,nz,horizontal;
        Plane(double[][] vertices) {
            this.vertices=vertices;
            double[] a=vertices[0],b=vertices[1],c=vertices[2];
            double ux=b[0]-a[0],uy=b[1]-a[1],uz=b[2]-a[2];
            double vx=c[0]-a[0],vy=c[1]-a[1],vz=c[2]-a[2];
            double x=uy*vz-uz*vy,y=uz*vx-ux*vz,z=ux*vy-uy*vx;
            double length=Math.sqrt(x*x+y*y+z*z);
            nx=x/length;ny=y/length;nz=z/length;horizontal=nx*nx+nz*nz;
        }
        double oldSide(int vertex,double x,double z) {
            double[] v=vertices[vertex];return (x-v[0])*nx+(z-v[2])*nz;
        }
        double side(int vertex,double x,double y,double z) {
            double[] v=vertices[vertex];
            return (x-WallPlaneAnchor.coordinate(v[0],v[1],nx,ny,horizontal,y))*nx+
                   (z-WallPlaneAnchor.coordinate(v[2],v[1],nz,ny,horizontal,y))*nz;
        }
    }
    private static void test(String name,Runnable body) {body.run();passed++;System.out.println("PASS: "+name);}
    private static void check(boolean value) {if(!value)throw new AssertionError("sloped wall side regression");}
    private static void near(double a,double b) {if(Math.abs(a-b)>1e-9)throw new AssertionError(a+" != "+b);}
}
