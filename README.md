# Flocking Behaviour in ECS:

# Description
This project uses ECS architecture, the Burst compiler, Job System, a spatial hashing algorithm, and vector fields to simulate a massive school of fish swimming around all at once.

# Development Process:
## Yesterdays Approach

Inspired by Craig Reynolds' [flocking model](https://www.red3d.com/cwr/boids), where each boid follows three basic behavior rules:

![image](https://github.com/user-attachments/assets/20464b40-1c5b-4bd8-a860-772d0975439e)

I played around with that in Unity a few years back, and I was honestly blown away by the cool flocking patterns that could emerge from just those three simple rules.

[<Video>](https://github.com/user-attachments/assets/7c15af21-9e48-4f9f-bf01-3d4059190d2c)

The main issue back then was that i run into performance issues.
To make a really nice-looking flock of sheep, you need a lot of sheep. But my implementation didn’t scale well.

- This version relied on a physics-based sensory system for each sheep, meaning they each cast a bunch of rays to detect nearby sheep, the dog, or obstacles.
- Every sheep calculated its behavior in real-time on the main thread, which meant updating them one by one..

So basically, every new sheep created a huge load of data to gain and process.
The only way to handle this performance bottleneck was to reduce the sensory range and precision.
But of course, that kind of tweaking hurts the overall quality of the flocking behavior. Classic Performance vs. Quality trade-off.

Back then, I was toying with the idea of offloading some of the behavior logic to the GPU using Compute Shaders - kind of a “multithreaded” workaround.
Sadly, that part never really happened. - You know how it is with projects that land on the "Unfinished" pile 😅

## Todays Approach
Just recently, while casually digging through my endless pile of unfinished projects, I stumbled across this old prototype again. It hit me with that weird “this can’t be it” feeling - and I knew I had to take another shot at doing it right.
This time around, the goal wasn’t just to make another boid simulation. I wanted to see how far I could push the number of boids while still keeping a ~24 FPS frame rate.
To make things a bit more interesting, I swapped out the sheep and went with a school of fish bringing vertical movement into the mix and making the whole simulation feel way more dynamic.

### ECS 
Since I wanted to finally check out Unity DOTS, I figured this was the perfect opportunity. First, I had to shift gears - from the usual OOP setup to an ECS architecture.
Ditching most of Unity’s built-in physics and going with a math-based system instead already gave a solid boost. Just from that change alone, I was able to run some hundrets of fish. Not bad for a starting point!

> [!NOTE]
> 600 Fish ~24 FPS

https://github.com/user-attachments/assets/1f711fa2-79ae-4239-95cb-f0b92ae292dc

### Burst Compiler
This is now where the power of combined DOTS comes to action. After throwing the Burst Compiler into the mix, things really started to pick up. I was already getting some thousands of fish swimming together smoothly 

> [!NOTE]
> 3650 Fish ~ 24 FPS

https://github.com/user-attachments/assets/4a394f19-0564-4a59-94cf-ab0db9f8a4c1

### Job System
Besides that we are still running single threaded and calculate each fish at a time. 
Thats where the Job system comes in place. Which allowes us to split this operation onto multiple workers which can work simultaneously allowing to push the number of fish into almost ten thousands.

> [!NOTE]
> 9000 Fish ~ 24 FPS

https://github.com/user-attachments/assets/cdd920e5-418f-4b11-b3ec-b0af265b391a

### Spatial Hashing
But let’s be real - there are folks out there running hundreds of thousands of boids smoothly. So I couldn’t just stop there.
The biggest performance bottleneck was still that each boid is looping through all other boids in the simulation. Worst part about that is that most of those processed boids were way out of range and didn’t actually affect behavior at all.
That’s when I stumbled across [this awesome video](https://www.youtube.com/watch?v=vxZx_PXo-yo&t=9s) and discovered spatial hashing - a method where you assign each position in space a hash value. You can then use that in a lookup table to quickly find nearby fish.

Now, instead of looping through all the fish, each one only checks those in neighboring positions.

> [!NOTE]
> 130,000 Fish ~ 24 FPS

https://github.com/user-attachments/assets/dafa42a7-ac3c-4283-abd8-6dca04094a0f

<!---
### Vector Fields
With all that achieved, the fishes where outnumbering the sheeps by far. Yet they were not living up to the sheeps inteligance. The sheeps could detect and avoid obstacles but my fish were just swimming thorugh anything in their path.
To not rely on unity physics and raycasting for hundrests of thousands of fish, i was inspire by the unity's new VFX system, which is GPU based and doesn't use unity physics system for collision detection but something knowen as SDFs and Vector fields, which can be pre baked from any mesh. 
To not make it boring, i wrote my own vector field baker tool. check it ot here. 
With that i am able to create vector fields of any game object in my scene. 

I created a little pakour for my fish, with some blocks, rings and pipes to play around.

### Adding some Interactions.
At this point, the problem was that i am running out of screen space to show all these fish and to live up to the expections of the prototype it was time to add some interaction to the simulation.
Where instead of the dog, i gave the player the ability to move as a free flying camera (or should i say free swimming camera) thoruhg these shools and frighten away any fish in his way.
Next up was obstacle detection. 
--->

