# Non-Euclidean Ray Marching
This repository contains an implementation of ray marching that can render in non-Euclidean spaces. 
## The Non-Euclidean Advantage
Rendering in non-euclidean spaces with ray marching is far easier than with traditional means such as rasterizing or raytracing because in ray marching we only need 3 things:
1. A way of representing points and rays
2. A way of calculating distances for SDFs
3. A way of moving a point along a ray by a certain distance

Typically, we would use euclidean implementations of these things. So, by simply swapping out the euclidean implementations for non-euclidean ones, we can render in non-euclidean spaces.
## Other Advantages
While ray marching is often slower than rasterization or raytracing, there are some scenes which are easy with ray marching but difficult or impossible with other methods. For example fractals, booleans, and smooth booleans are often more straightforward to implement with ray marching. This repository also includes some euclidean scenes showcasing smooth unions and intersections which would be difficult to implement with other rendering methods.
## Instructions
- Use the arrow keys to switch between scenes
- Hold left click to rotate the orbit cameras
- Use the scroll wheel to zoom in and out in the orbit cameras
- Use WASD in the first-person scenes (holding LMB is not required to rotate the camera)