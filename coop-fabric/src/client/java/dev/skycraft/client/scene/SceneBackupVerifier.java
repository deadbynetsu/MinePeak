package dev.skycraft.client.scene;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.HashSet;
import java.util.Set;
import java.util.zip.CRC32;
import java.util.zip.ZipFile;

/** Verifies the newly completed native Minecraft backup before opening an experimental scene. */
public final class SceneBackupVerifier {
    private SceneBackupVerifier() { }

    public static Set<Path> existing(Path directory) throws IOException {
        Files.createDirectories(directory);
        try (var files = Files.list(directory)) {
            Set<Path> result = new HashSet<>();
            files.filter(SceneBackupVerifier::archive).map(path -> path.toAbsolutePath().normalize()).forEach(result::add);
            return result;
        }
    }

    public static Path verifyNew(Path directory, Set<Path> previous) throws IOException {
        Set<Path> added = existing(directory); added.removeAll(previous);
        if (added.size() != 1) throw new IOException("The new Minecraft world backup could not be identified safely");
        Path file = added.iterator().next();
        boolean levelData = false; int entries = 0; long expanded = 0;
        byte[] buffer = new byte[32 * 1024];
        try (ZipFile zip = new ZipFile(file.toFile())) {
            var files = zip.entries();
            while (files.hasMoreElements()) {
                var entry = files.nextElement();
                if (entry.isDirectory()) continue;
                entries++;
                levelData |= entry.getName().equals("level.dat") || entry.getName().endsWith("/level.dat");
                CRC32 crc = new CRC32(); long size = 0;
                try (var input = zip.getInputStream(entry)) {
                    for (int n; (n = input.read(buffer)) >= 0;) {
                        size += n; expanded += n; crc.update(buffer, 0, n);
                        if (expanded > 8L * 1024 * 1024 * 1024) throw new IOException("World backup exceeds the verification limit");
                    }
                }
                if (size != entry.getSize() || crc.getValue() != entry.getCrc())
                    throw new IOException("World backup entry failed size/CRC verification");
            }
        }
        if (entries == 0 || !levelData) throw new IOException("World backup is empty or missing level.dat");
        return file;
    }

    private static boolean archive(Path path) { return Files.isRegularFile(path) && path.getFileName().toString().endsWith(".zip"); }
}
