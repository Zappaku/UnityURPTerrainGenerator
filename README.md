# Unity URP Procedural Terrain Generator
## Unity Version Required: Unity 2022.3.6f1 or above </br>

### Importing The Tool Into Your Project:
● Download the latest .unityproject file from the Releases </br>
● Open Your Unity Project & Import Package </br>

### Setting Up The Tool:
● Open the “Prefabs” folder and drag the TerrainGenerator prefab into your scene

### Using The Tool & Tool Settings:
● Presets - Basic Generation Preset </br>
- Grasslands: Generates landscapes with hills and smaller water bodies. </br>
- Desert: Generates deserts and dunes with no water bodies. </br>
- Mountainous: Generates landscapes with taller hills/mountains and small water bodies. </br>
- Lake: Generates landscapes and small hills with larger water bodies. Also, generates a set number of lakes and lake sizes. </br> </br>

● Terrain Layers
- Put Terrain Layers here. These will be automatically painted onto the terrain.
These layers are preset-dependent. For example, the desert preset takes a different
main terrain layer than the grasslands preset. </br>
- Main Terrain Layer: This is the primary terrain layer. It will be painted over most of the terrain.
By default, it is a grass material. </br>
- Shoreline/Underwater Layer: This is the underwater terrain layer. It will be painted around and under
water bodies. By default, it is a sand material. </br> </br>

● Size/Resolution
- Control the size, resolution, and depth of the terrain that will be generated. </br>
- Width/Height: This controls the size (width and height) of the terrain. By default, it is
1024x1024. Larger terrain sizes will have a larger resolution. </br>
- Depth: This controls the overall amplitude of the terrain. Larger values produce
more extreme hills, while smaller values produce flatter terrain. </br>
- Scale: This controls the scale of the terrain. Larger values generate terrain closer
together, while smaller values generate terrain further apart, looking more
expansive. </br> </br>

● Water Bodies
- Controls the settings for the generated water bodies on the terrain. </br>
- Water Level Height: This controls the height at which the water level will start. </br>
- Water Material: This is the material that will go onto the generated water plane. </br>
- Number of Lakes: While the lake preset is selected, this controls how many lake water bodies
will be generated. </br>
- Lake Radius: While the lake preset is selected, this controls how large the lakes will be. </br> </br>

● Seed
- Select whether you want to generate a terrain from a random or set seed. </br>
- Use Random Seed: When checked, the terrain will generate from a random seed every time it
is generated. </br>
- Set Seed: When “Use Random Seed” is unchecked, this value controls the seed from
which the terrain is generated. </br> </br>

● Generate
- Click to generate the terrain! Existing terrains that were generated with this tool
will be automatically deleted and replaced. </br>
