package dev.skycraft.client;

import com.google.gson.JsonArray;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import dev.skycraft.SkyCraft;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.RandomAccessFile;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.charset.StandardCharsets;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import net.minecraft.client.Minecraft;
import net.minecraft.client.server.IntegratedServer;
import net.minecraft.network.chat.Component;
import net.minecraft.network.chat.ClickEvent.CopyToClipboard;
import org.jspecify.annotations.Nullable;

public final class DiscordPresence {
   private static final String APP_ID = System.getProperty("skycraft.discordAppId", "1554889902309642280");
   private static final Pattern E4MC_LINK = Pattern.compile("[a-z0-9-]+(?:\\.[a-z0-9-]+)*\\.e4mc\\.link", 2);
   private static final int OP_HANDSHAKE = 0;
   private static final int OP_FRAME = 1;
   private static final int OP_CLOSE = 2;
   private static final int OP_PING = 3;
   private static final int OP_PONG = 4;
   private static final long START = System.currentTimeMillis() / 1000L;
   private static volatile @Nullable String hostLink;
   private static volatile @Nullable String wantedActivity;
   private static long nextDescribe;
   private static @Nullable RandomAccessFile pipe;
   private static @Nullable FileInputStream in;
   private static @Nullable String sentActivity;
   private static long lastSent;
   private static boolean loggedNoDiscord;
   private static int nonce;

   private DiscordPresence() {
   }

   public static void start() {
      if (!"0".equals(APP_ID) && !APP_ID.isBlank()) {
         Thread var0 = new Thread(DiscordPresence::run, "SkyCraft Discord");
         var0.setDaemon(true);
         var0.start();
      } else {
         SkyCraft.LOG.info("SkyCraft: Discord Rich Presence off (no application id)");
      }
   }

   public static void onChat(Component var0) {
      String var1 = findLink(var0);
      if (var1 != null) {
         hostLink = var1;
         SkyCraft.LOG.info("SkyCraft: this world is open to friends at {}", var1);
      }
   }

   private static @Nullable String findLink(Component var0) {
      Matcher var1 = E4MC_LINK.matcher(var0.getString());
      if (var1.find()) {
         return var1.group().toLowerCase();
      }

      for (Component var3 : var0.toFlatList()) {
         if (var3.getStyle().getClickEvent() instanceof CopyToClipboard var4) {
            var1 = E4MC_LINK.matcher(var4.value());
            if (var1.find()) {
               return var1.group().toLowerCase();
            }
         }
      }

      return null;
   }

   public static void tick(Minecraft var0) {
      long var1 = System.currentTimeMillis();
      if (var1 >= nextDescribe) {
         nextDescribe = var1 + 1000L;
         IntegratedServer var3 = var0.getSingleplayerServer();
         if (var3 == null || !var3.isPublished()) {
            hostLink = null;
         }

         JsonObject var4 = null;
         if (var0.player != null && var0.getConnection() != null) {
            int var5 = var0.getConnection().getOnlinePlayers().size();
            String var6 = MirrorWorld.friendAddress(var0);
            var4 = new JsonObject();
            var4.addProperty("details", "Playing Skyrim as a Minecraft player");
            JsonObject var7 = new JsonObject();
            var7.addProperty("start", START);
            var4.add("timestamps", var7);
            JsonObject var8 = new JsonObject();
            var8.addProperty("large_image", "skycraft");
            var8.addProperty("large_text", "SkyCraft");
            var4.add("assets", var8);
            String var9 = hostLink != null ? hostLink : var6;
            if (hostLink != null) {
               var4.addProperty("state", "Hosting a world");
            } else if (var6 != null) {
               var4.addProperty("state", "In a friend's world");
            } else {
               var4.addProperty("state", "Playing solo");
            }

            if (var9 != null) {
               JsonObject var10 = new JsonObject();
               var10.addProperty("id", "skycraft-" + Integer.toHexString(var9.hashCode()));
               JsonArray var11 = new JsonArray();
               var11.add(Math.max(var5, 1));
               var11.add(var3 != null ? Math.max(var3.getMaxPlayers(), var5 + 1) : Math.max(100, var5 + 1));
               var10.add("size", var11);
               var4.add("party", var10);
               JsonObject var12 = new JsonObject();
               var12.addProperty("join", var9);
               var4.add("secrets", var12);
            }
         }

         JsonObject var13 = new JsonObject();
         var13.addProperty("pid", ProcessHandle.current().pid());
         var13.add("activity", var4);
         wantedActivity = var13.toString();
      }
   }

   private static void run() {
      while (true) {
         try {
            if (pipe == null && !connect()) {
               Thread.sleep(15000L);
            } else {
               String var0 = wantedActivity;
               long var1 = System.currentTimeMillis();
               if (var0 != null && !var0.equals(sentActivity) && var1 - lastSent > 5000L) {
                  command("SET_ACTIVITY", JsonParser.parseString(var0).getAsJsonObject(), null);
                  sentActivity = var0;
                  lastSent = var1;
               }

               readAvailable();
               Thread.sleep(250L);
            }
         } catch (IOException var3) {
            SkyCraft.LOG.info("SkyCraft: Discord connection closed ({}); retrying later", var3.getMessage());
            close();
         } catch (InterruptedException var4) {
            return;
         } catch (RuntimeException var5) {
            SkyCraft.LOG.warn("SkyCraft: Discord Rich Presence error", var5);
            close();
         }
      }
   }

   private static boolean connect() throws IOException, InterruptedException {
      int var0 = 0;

      while (var0 < 10) {
         try {
            pipe = new RandomAccessFile("\\\\.\\pipe\\discord-ipc-" + var0, "rw");
         } catch (IOException var5) {
            var0++;
            continue;
         }

         in = new FileInputStream(pipe.getFD());
         JsonObject var1 = new JsonObject();
         var1.addProperty("v", 1);
         var1.addProperty("client_id", APP_ID);
         write(0, var1);
         long var2 = System.currentTimeMillis() + 5000L;

         while (System.currentTimeMillis() < var2) {
            JsonObject var4 = readFrame();
            if (var4 != null && "READY".equals(str(var4, "evt"))) {
               command("SUBSCRIBE", null, "ACTIVITY_JOIN");
               SkyCraft.LOG.info("SkyCraft: connected to Discord (Rich Presence)");
               loggedNoDiscord = false;
               sentActivity = null;
               return true;
            }

            if (var4 == null) {
               Thread.sleep(100L);
            }
         }

         close();
         return false;
      }

      if (!loggedNoDiscord) {
         loggedNoDiscord = true;
         SkyCraft.LOG.info("SkyCraft: Discord isn't running; Rich Presence will connect when it is");
      }

      return false;
   }

   private static void command(String var0, @Nullable JsonObject var1, @Nullable String var2) throws IOException {
      JsonObject var3 = new JsonObject();
      var3.addProperty("cmd", var0);
      var3.addProperty("nonce", Integer.toString(++nonce));
      if (var1 != null) {
         var3.add("args", var1);
      }

      if (var2 != null) {
         var3.addProperty("evt", var2);
      }

      write(1, var3);
   }

   private static void readAvailable() throws IOException {
      JsonObject var0;
      while ((var0 = readFrame()) != null) {
         if ("DISPATCH".equals(str(var0, "cmd")) && "ACTIVITY_JOIN".equals(str(var0, "evt")) && var0.has("data")) {
            String var1 = str(var0.getAsJsonObject("data"), "secret");
            if (var1 != null && E4MC_LINK.matcher(var1).matches()) {
               SkyCraft.LOG.info("SkyCraft: joining {} from Discord", var1);
               Minecraft.getInstance().execute(() -> MirrorWorld.joinFriend(Minecraft.getInstance(), var1));
            }
         } else if ("ERROR".equals(str(var0, "evt"))) {
            SkyCraft.LOG.warn("SkyCraft: Discord says {}", var0.get("data"));
         }
      }
   }

   private static void write(int var0, JsonObject var1) throws IOException {
      byte[] var2 = var1.toString().getBytes(StandardCharsets.UTF_8);
      ByteBuffer var3 = ByteBuffer.allocate(8 + var2.length).order(ByteOrder.LITTLE_ENDIAN);
      var3.putInt(var0).putInt(var2.length).put(var2);
      pipe.write(var3.array());
   }

   private static @Nullable JsonObject readFrame() throws IOException {
      while (in.available() >= 8) {
         byte[] var0 = new byte[8];
         pipe.readFully(var0);
         ByteBuffer var1 = ByteBuffer.wrap(var0).order(ByteOrder.LITTLE_ENDIAN);
         int var2 = var1.getInt();
         int var3 = var1.getInt();
         byte[] var4 = new byte[var3];
         pipe.readFully(var4);
         String var5 = new String(var4, StandardCharsets.UTF_8);
         switch (var2) {
            case 1:
               return JsonParser.parseString(var5).getAsJsonObject();
            case 2:
               throw new IOException("Discord closed the connection: " + var5);
            case 3:
               ByteBuffer var6 = ByteBuffer.allocate(8 + var3).order(ByteOrder.LITTLE_ENDIAN);
               var6.putInt(4).putInt(var3).put(var4);
               pipe.write(var6.array());
         }
      }

      return null;
   }

   private static @Nullable String str(JsonObject var0, String var1) {
      return var0.has(var1) && var0.get(var1).isJsonPrimitive() ? var0.get(var1).getAsString() : null;
   }

   private static void close() {
      try {
         if (pipe != null) {
            pipe.close();
         }
      } catch (IOException var1) {
      }

      pipe = null;
      in = null;
      sentActivity = null;
   }
}
