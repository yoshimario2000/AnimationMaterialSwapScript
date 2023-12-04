# AnimationMaterialSwapScript
A tool for swapping out materials in an animator

Found in engine at `Tools->Yoshark Tools>Animator Tool`

---

## Usage

Takes an animator controller, paths to where you want to put the new files, and will scan it for any animations with material definions within.

Under materials to swap, on the left are listed the existing materials in the animations, on the right are what the animations to be replaced with. Null materials in the list will leave the material and animation as is. any unmodified animations will not be dupplicated.