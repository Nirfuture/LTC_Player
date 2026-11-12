# Local startup comparison (2026-11-13)

Source: f2d9ce7, no code changes. Dedicated verification build, three sequential starts, exit code 0. No full suite or performance changes.

Material SHA256: ed948fb91e7b24abbc634808f51c19cdfa9174cf0370776326bcb28138ab97bd (matches company report).

Local conditions: i7-13700KF / RTX 4080; FFmpeg 6.0-full_build-www.gyan.dev; Realtek speakers; canvas 1920x1080; external output off; hardware gain 0; device initialized before play. Company conditions differ: FFmpeg 8.1, NVIDIA HDMI audio, canvas 1080x1920. OS cache not cleared. Three trials are diagnostic samples, not a statistical benchmark.

Milliseconds from play_request, rounded:

| Metric | Local first | Local retry 1 | Local retry 2 | Company first | Company retry 1 | Company retry 2 |
|---|---:|---:|---:|---:|---:|---:|
| Still presentation API return | 135 | 79 | 84 | 1695 | 624 | 616 |
| Playback presentation API return | 209 | 146 | 149 | 2308 | 1218 | 1208 |
| WASAPI Start return | 290 | 184 | 188 | 3426 | 1782 | 1762 |
| Audio FFmpeg start request to Process.Start return | 4 | 12 | 4 | 501 | 499 | 491 |

Local audio internal error after start: +3.65, -0.32, -7.81 ms; zero reseeks. Physical output timing and long-term synchronization were not measured.

Finding: large company-specific delay exists before Process.Start returns. This does not isolate OS process creation, security inspection, executable build/location, or scheduling. FFmpeg version differs, so CPU/Boost cannot be identified as the cause. Both PCs also follow the same sequential still/stream preparation path.

Next bounded diagnostic: on company PC, measure the same .NET launch path for ffmpeg -version a few times, separating Process.Start return from process exit. Then compare the same FFmpeg binary/version on both PCs if needed. Do not change security settings. No additional tests performed here.

Raw JSON remains outside the repository under the Codex workspace work/startup-comparison/run-1. No generated binaries or raw device settings added to Git.

## Single-buffer playback change (2026-11-13, uncommitted)

PlayAsync now consumes and presents the first frame from its continuous playback buffer before starting the clock. The separate restart still decode is removed. LoadAsync and SeekAsync still presentation are unchanged.

Validation: verification build succeeded; preview-performance passed (6 measurements including HAP/H.264 pause/seek/replay); playback-integration 11/11 passed; startup harness 3/3 completed, zero reseeks. No full suite executed.

Restart trials contain no video_still_decode_begin event. Request to WASAPI Start was 106/108ms (previous local baseline 184/188ms). First attempt was 283ms versus prior 290ms; initial selection still decoding remains. These are small samples, not guaranteed latency bounds. Company-PC improvement remains unmeasured.

Measurement caveat: video_first_present records the buffer's first frame before clock start on restarts. video_playback_first_present is emitted by RunAsync on a later changed frame, so after this change it is not the first presentation from the playback buffer. Do not compare that marker alone as equivalent first-frame latency across versions. Raw transitions preserve both markers and video_clock_start.
