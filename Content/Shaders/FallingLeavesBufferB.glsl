/* ──────────────────────────────────────────────────────────────
   FALLING LEAVES — Buffer B (bokeh depth-of-field) — ShaderToy

   Blurs Buffer A with a disk (bokeh) kernel whose radius grows with
   distance from the focus point — cheap fake depth-of-field.

   iChannel0 = Buffer A (FallingLeavesBufferA.glsl).

   ShaderToy multi-buffer wiring:
     Buffer A : FallingLeavesBufferA.glsl. iChannel0 = background.
     Buffer B : this file. iChannel0 = Buffer A.
     Image    : FallingLeavesImage.glsl. iChannel0 = Buffer B.

   Paste into the Buffer B tab, bind iChannel0 = Buffer A, Alt+Enter.
   ────────────────────────────────────────────────────────────── */

// ═══════════════════════════════════════════════════════════════
// TUNABLES
// ═══════════════════════════════════════════════════════════════

#define BOKEH_N 6  // disk samples per axis. 4 = cheap/chunky, 9 = smooth/heavy GPU

const float BLUR_STRENGTH = 0.05;  // overall DoF strength. 0 = pin sharp, 0.1 = dreamy mush
const vec2  FOCUS_A       = vec2(0.6);  // blur grows with distance from here... (one focal anchor)
const vec2  FOCUS_B       = vec2(0.5);  // ...and from here (two anchors shape the in-focus band)
const vec2  BOKEH_ASPECT  = vec2(9.0 / 16.0, 1.0);  // keeps the blur disk round on a 16:9 frame

// ═══════════════════════════════════════════════════════════════
// HELPERS
// ═══════════════════════════════════════════════════════════════

// bokeh: average a disk of taps, radius = b
vec4 bokeh(sampler2D sam, vec2 p, float b)
{
    vec4 col = vec4(0.0);
    for (int i = -BOKEH_N; i <= BOKEH_N; i++)
    for (int j = -BOKEH_N; j <= BOKEH_N; j++) {
        vec2 off = vec2(float(i), float(j)) / float(BOKEH_N);
        if (dot(off, off) < 1.0) {  // inside the disk
            col += texture(sam, p + b * off * BOKEH_ASPECT);
        }
    }
    return col / col.a;
}

// ═══════════════════════════════════════════════════════════════
// MAIN IMAGE
// ═══════════════════════════════════════════════════════════════

void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    vec2 p = fragCoord / iResolution.xy;
    // blur radius rises with distance from the focus anchors
    float b = dot(p - FOCUS_A, p - FOCUS_B) * BLUR_STRENGTH;
    vec3 col = bokeh(iChannel0, p, b).rgb;
    fragColor = vec4(col, 1.0);
}
