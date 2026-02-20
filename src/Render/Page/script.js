//Thx ChatGPT, I don't know JavaScript nor Tree.js

const canvas = document.querySelector('.webgl')
const scene = new THREE.Scene()
scene.background = new THREE.Color(0x202020);
const textureLoader = new THREE.TextureLoader()
const sizes = {
    width: window.innerWidth,
    height: window.innerHeight
}

// Base camera
const camera = new THREE.PerspectiveCamera(60, window.innerWidth/window.innerHeight, 0.1, 5000);
camera.position.set(60, 60, 160);
scene.add(camera)

// Controls
const controls = new THREE.OrbitControls(camera, canvas)
controls.enableDamping = true
controls.enableZoom = true
controls.minDistance = 10
controls.maxDistance = 100

// Renderer
const renderer = new THREE.WebGLRenderer({
    canvas: canvas,
    antialias: true,
    alpha: true
})

renderer.setSize(sizes.width, sizes.height)
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
renderer.outputEncoding = THREE.sRGBEncoding

scene.add(new THREE.DirectionalLight(0xffffff, 1.2));
scene.add(new THREE.AmbientLight(0x404040));

let mesh = null;

/* ------------------------------
   Greedy Meshing
--------------------------------*/

function greedyMesh(voxels) {
    const positions = [];
    const colors = [];
    const indices = [];
    let indexOffset = 0;

    const voxelKeys = Object.keys(voxels)
        .map(k => k.split(',').map(Number)); // [[x,y,z], ...]

    const directions = [
        [1,0,0], [-1,0,0],
        [0,1,0], [0,-1,0],
        [0,0,1], [0,0,-1]
    ];

    const faceVertices = [
        [[1,0,0],[1,1,0],[1,1,1],[1,0,1]], // +X
        [[0,0,1],[0,1,1],[0,1,0],[0,0,0]], // -X
        [[0,1,1],[1,1,1],[1,1,0],[0,1,0]], // +Y
        [[0,0,0],[1,0,0],[1,0,1],[0,0,1]], // -Y
        [[0,0,1],[1,0,1],[1,1,1],[0,1,1]], // +Z
        [[0,1,0],[1,1,0],[1,0,0],[0,0,0]]  // -Z
    ];

    const voxelSet = new Set(Object.keys(voxels));

    for (const [x, y, z] of voxelKeys) {
        const color = voxels[`${x},${y},${z}`];

        for (let f = 0; f < 6; f++) {
            const nx = x + directions[f][0];
            const ny = y + directions[f][1];
            const nz = z + directions[f][2];

            if (voxelSet.has(`${nx},${ny},${nz}`)) continue; // skip hidden faces

            // Add face vertices
            for (let i = 0; i < 4; i++) {
                const v = faceVertices[f][i];
                positions.push(x+v[0], y+v[1], z+v[2]);
                colors.push(...color);
            }

            indices.push(
                indexOffset, indexOffset+1, indexOffset+2,
                indexOffset, indexOffset+2, indexOffset+3
            );
            indexOffset += 4;
        }
    }

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(positions, 3));
    geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors, 3));
    geometry.setIndex(indices);
    geometry.computeVertexNormals();

    return geometry;
}

// File input
document.getElementById('fileInput').addEventListener('change', (event) => {
    const file = event.target.files[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = function(e) {
        const data = JSON.parse(e.target.result);
        if(mesh) scene.remove(mesh);
        const geometry = greedyMesh(data);
        const material = new THREE.MeshLambertMaterial({ vertexColors:true });
        mesh = new THREE.Mesh(geometry, material);
        scene.add(mesh);
    };
    reader.readAsText(file);
});

window.addEventListener('resize', () =>
{
    sizes.width = window.innerWidth
    sizes.height = window.innerHeight
    camera.aspect = sizes.width / sizes.height
    camera.updateProjectionMatrix()
    renderer.setSize(sizes.width, sizes.height)
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
})
var button = document.getElementById("goto");
button.onclick = function ()
{
	var x = parseInt(document.getElementById("x").value);
	var y = parseInt(document.getElementById("y").value);
	var z = parseInt(document.getElementById("z").value);

	camera.position.set(x,z,y)
}
// Animation
const tick = () => {
    controls.update()
    renderer.render(scene, camera)
    window.requestAnimationFrame(tick)
}

tick()
