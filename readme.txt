Team members: (names and student IDs)
* Philip Tap 4735781
* ...
* ...

Tick the boxes below for the implemented features. Add a brief note only if necessary, e.g., if it's only partially working, or how to turn it on.

Formalities:
[X] This readme.txt
[ ] Cleaned (no obj/bin folders)

Minimum requirements implemented:
[X] Camera: position and orientation controls, field of view in degrees
Controls: 1 for OpenGL Path tracer, 2, 3, and 4 for debug view from different axes.
Controls: Camera: move with WASD, space for up, ctrl for down, shift for speed up and arrow keys for changing view direction.
Controls: M for controlling view direction with mouse. X and Z for zoom, F and G for quick zoom.
[X] Primitives: plane, sphere
[X] Lights: at least 2 point lights, additive contribution, shadows without "acne"
[X] Diffuse shading: (N.L), distance attenuation
[X] Phong shading: (R.V) or (N.H), exponent
[X] Diffuse color texture: only required on the plane primitive, image or procedural, (u,v) texture coordinates
[X] Mirror reflection: recursive
[X] Debug visualization: sphere primitives, rays (primary, shadow, reflected, refracted)

Bonus features implemented:
[X] Triangle primitives: single triangles or meshes
[ ] Interpolated normals: only required on triangle primitives, 3 different vertex normals must be specified
[ ] Spot lights: smooth falloff optional
[X] Glossy reflections: not only of light sources but of other objects
[X] Anti-aliasing
[ ] Parallelized: using parallel-for, async tasks, threads, or [fill in other method]
[X] Textures: on all implemented primitives
[ ] Bump or normal mapping: on all implemented primitives
[ ] Environment mapping: sphere or cube map, without intersecting actual sphere/cube/triangle primitives
[ ] Refraction: also requires a reflected ray at every refractive surface, recursive
[X] Area lights: soft shadows
[X] Acceleration structure: bounding box or hierarchy, scene with 5000+ primitives
Note: [provide one measurement of speed/time with and without the acceleration structure]
[X] GPU implementation: using a fragment shader with OpenGL

Notes:
We have an extra feature to import simple .obj files to render more complex 3D models (that is how we loaded in the teapot).
We implemented an R*-Tree for our acceleration structure, we used wikipedia (https://en.wikipedia.org/wiki/R*-tree) as a source.
The checking of intersections with bounding boxes uses the proposed approach in the slides, but with a more optimized code implementation mentioned here: https://tavianator.com/2011/ray_box.html
The R*-tree speeds up a 'large' scene of around 6000 primitives with 6 light sources and a reflective floor by 3x times from average of 300 milliseconds to load a frame to an average of 110 milliseconds.
