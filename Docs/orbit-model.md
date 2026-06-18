# Simple Circular Orbit Model

For the purposes of this project, the satellite orbital positions produced by the orbital simulator will be simplified. Instead of the more realistic SGP4 propagation model, we use a simple circular orbit model which provides plausible continuous lat/lon/alt coordinates for the simulated satellites.

| Value | Symbol | Unit | Formula |
|-|-|-|-|
|-|-|-|-|
| Orbital Period | T | seconds | 2π * √((R + h)³ / GM) |