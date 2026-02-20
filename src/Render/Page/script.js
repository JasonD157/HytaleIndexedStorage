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
   Sample 32x32x64 grid
--------------------------------*/

const SIZE_X = 32;
const SIZE_Y = 320;
const SIZE_Z = 32;

function key(x, y, z) { return `${x},${y},${z}`; }

/* ------------------------------
   Greedy Meshing
--------------------------------*/

function greedyMesh(voxels, sizeX, sizeY, sizeZ)
{
    const positions = [];
    const colors = [];
    const indices = [];
    let indexOffset = 0;

    const dims = [sizeX, sizeY, sizeZ];
    const mask = [];

    for (let d=0; d<3; d++)
    {
        const u = (d+1)%3;
        const v = (d+2)%3;

        const x = [0,0,0];
        const q = [0,0,0];
        q[d] = 1;

        for (x[d]=-1; x[d]<dims[d]; )
        {
            let n = 0;

            for (x[v]=0; x[v]<dims[v]; x[v]++)
            for (x[u]=0; x[u]<dims[u]; x[u]++)
            {
                const a = (x[d]>=0)
                    ? voxels[key(x[0],x[1],x[2])]
                    : null;

                const b = (x[d]<dims[d]-1)
                    ? voxels[key(x[0]+q[0],x[1]+q[1],x[2]+q[2])]
                    : null;

                if (!!a === !!b)
                    mask[n++] = null;
                else
                    mask[n++] = a ? {color:a, back:false} : {color:b, back:true};
            }

            x[d]++;

            n = 0;

            for (let j=0; j<dims[v]; j++)
            for (let i=0; i<dims[u]; )
            {
                const m = mask[n];
                if (!m){ i++; n++; continue; }

                let w;
                for (w=1; i+w<dims[u] && mask[n+w] &&
                    JSON.stringify(mask[n+w].color)===JSON.stringify(m.color) &&
                    mask[n+w].back===m.back; w++);

                let h;
                outer: for (h=1; j+h<dims[v]; h++)
                {
                    for (let k=0;k<w;k++)
                    {
                        const next = mask[n+k+h*dims[u]];
                        if (!next ||
                            JSON.stringify(next.color)!==JSON.stringify(m.color) ||
                            next.back!==m.back)
                            break outer;
                    }
                }

                x[u]=i; x[v]=j;
                const du=[0,0,0]; const dv=[0,0,0];
                du[u]=w; dv[v]=h;

                const quad = [
                    [x[0],x[1],x[2]],
                    [x[0]+du[0],x[1]+du[1],x[2]+du[2]],
                    [x[0]+du[0]+dv[0],x[1]+du[1]+dv[1],x[2]+du[2]+dv[2]],
                    [x[0]+dv[0],x[1]+dv[1],x[2]+dv[2]]
                ];

                if (m.back) quad.reverse();

                for (let p of quad)
                {
                    positions.push(...p);
                    colors.push(...m.color);
                }

                indices.push(
                    indexOffset, indexOffset+1, indexOffset+2,
                    indexOffset, indexOffset+2, indexOffset+3
                );
                indexOffset+=4;

                for (let l=0;l<h;l++)
                for (let k=0;k<w;k++)
                    mask[n+k+l*dims[u]]=null;

                i+=w;
                n+=w;
            }
        }
    }

    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position',
        new THREE.Float32BufferAttribute(positions,3));
    geometry.setAttribute('color',
        new THREE.Float32BufferAttribute(colors,3));
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
        const geometry = greedyMesh(data, SIZE_X, SIZE_Y, SIZE_Z);
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
// Animation
const tick = () => {
    controls.update()
    renderer.render(scene, camera)
    window.requestAnimationFrame(tick)
}

tick()
