const canvas = document.querySelector('.webgl');
const scene = new THREE.Scene();
scene.background = new THREE.Color(0x141520);

const sizes = { width: window.innerWidth, height: window.innerHeight };

// Perspective camera — starts directly overhead
const camera = new THREE.PerspectiveCamera(60, sizes.width / sizes.height, 0.1, 10000);
scene.add(camera);

// OrbitControls:
//   Left-drag  = pan
//   Right-drag = rotate
//   Scroll     = zoom
const controls = new THREE.OrbitControls(camera, canvas);
controls.enableDamping = true;
controls.dampingFactor = 0.1;
controls.enablePan = true;
controls.screenSpacePanning = false;
controls.mouseButtons.LEFT  = THREE.MOUSE.PAN;
controls.mouseButtons.RIGHT = THREE.MOUSE.ROTATE;
// Prevent going underground
controls.maxPolarAngle = 0;

// Keyboard movement
const keys = {};
const moveSpeed = 0.8;

document.addEventListener("keydown", (e) => {
    keys[e.code] = true;
});

document.addEventListener("keyup", (e) => {
    keys[e.code] = false;
});

// Lighting
scene.add(new THREE.DirectionalLight(0xffffff, 1.2));
scene.add(new THREE.AmbientLight(0x404040));

// ── Tooltip (off-thread via raycastWorker) ────────────────────────────────────
const tooltip = document.getElementById('tooltip');

let voxelData    = null;
let mousePixel   = null;
let mouseOnCanvas = false;

// Pending cast: only one in-flight at a time to avoid result pile-up
let castPending  = false;
let castIdCounter = 0;
let latestCastId  = -1;

const rayWorker = new Worker('./raycastWorker.js');

rayWorker.onmessage = function(e) {
    const msg = e.data;
    castPending = false;

    // Discard stale replies
    if (msg.id !== latestCastId) return;

    if (!msg.hit || !voxelData) { tooltip.style.display = 'none'; return; }

    const bx = Math.floor(msg.px - msg.nx * 0.5);
    const by = Math.floor(msg.py - msg.ny * 0.5);
    const bz = Math.floor(msg.pz - msg.nz * 0.5);
    const color = voxelData[`${bx},${by},${bz}`];

    if (color && mouseOnCanvas && mousePixel && !mouseDown) {
        tooltip.style.display = 'block';
        tooltip.style.left = (mousePixel.x + 14) + 'px';
        tooltip.style.top  = (mousePixel.y - 10) + 'px';
        tooltip.innerHTML =
            `<span class="tip-coord">X:&nbsp;${bx} &nbsp; Y:&nbsp;${by} &nbsp; Z:&nbsp;${bz}</span><br>` +
            `<span class="tip-swatch" style="background:${color}"></span> ${color.toUpperCase()}`;
    } else {
        tooltip.style.display = 'none';
    }
};

let mouseDown = false;
canvas.addEventListener('pointerdown', () => { mouseDown = true; tooltip.style.display = 'none'; });
canvas.addEventListener('pointerup',   () => { mouseDown = false; });

canvas.addEventListener('mousemove', (e) => {
    mousePixel    = { x: e.clientX, y: e.clientY };
    mouseOnCanvas = true;
});
canvas.addEventListener('mouseleave', () => {
    mouseOnCanvas = false;
    tooltip.style.display = 'none';
});

// Called each frame — fires a cast only if no reply is pending and mouse is up
function updateTooltip() {
    if (!mesh || !voxelData || !mouseOnCanvas || !mousePixel || castPending || mouseDown) return;

    const rect = canvas.getBoundingClientRect();
    const ndcX =  ((mousePixel.x - rect.left) / rect.width)  * 2 - 1;
    const ndcY = -((mousePixel.y - rect.top)  / rect.height) * 2 + 1;

    // Unproject two points to build a world-space ray (same math as THREE.Raycaster)
    const near = new THREE.Vector3(ndcX, ndcY, -1).unproject(camera);
    const far  = new THREE.Vector3(ndcX, ndcY,  1).unproject(camera);
    const dir  = far.sub(near).normalize();

    const id = ++castIdCounter;
    latestCastId = id;
    castPending  = true;

    rayWorker.postMessage({
        type: 'cast', id,
        ox: near.x, oy: near.y, oz: near.z,
        dx: dir.x,  dy: dir.y,  dz: dir.z
    });
}

// ── meshWorker (identical to voxelviewer) ─────────────────────────────────────
let mesh = null;
const worker = new Worker('./meshWorker.js');

let currentLoadingPercentage = 0;
const FileLoadPercentage    = 10;
const JsonParsePercentage   = 30;
const VoxelParsePercentage  = 30;

function setLoadingBar(pct) {
    currentLoadingPercentage = pct;
    loadingBar.style.width = pct + '%';
}
function incrementLoadingBar(pct) {
    setLoadingBar(currentLoadingPercentage + pct);
}

worker.onmessage = function (e) {
    if (e.data.progress !== undefined) {
        loadingBar.style.width = (currentLoadingPercentage + e.data.progress * VoxelParsePercentage) + '%';
    }

    if (e.data.done) {
        setLoadingBar(80);

        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute('position', new THREE.Float32BufferAttribute(e.data.positions, 3));
        geometry.setAttribute('color',    new THREE.Float32BufferAttribute(e.data.colors, 3));
        setLoadingBar(85);
        geometry.setIndex(e.data.indices);
        geometry.computeVertexNormals();
        setLoadingBar(90);

        const material = new THREE.MeshLambertMaterial({ vertexColors: true });
        mesh = new THREE.Mesh(geometry, material);
        setLoadingBar(95);
        scene.add(mesh);

        // Send geometry to raycast worker (transfer ownership — zero copy)
        const posArray = new Float32Array(e.data.positions);
        const idxArray = new Uint32Array(e.data.indices);
        rayWorker.postMessage(
            { type: 'init', positions: posArray, indices: idxArray },
            [posArray.buffer, idxArray.buffer]
        );

        // Place camera directly above the first voxel, looking straight down.
        // Slight Z offset (0.01) avoids OrbitControls' polar=0 singularity.
        const cx = e.data.x + 0.5;
        const cy = e.data.y + 0.5;
        const cz = e.data.z + 0.5;
        controls.target.set(cx, cy, cz);
        camera.position.set(cx, cy + 500, cz + 0.01);
        controls.update();

        requestAnimationFrame(() => {
            renderer.render(scene, camera);
            setLoadingBar(100);
            setTimeout(() => {
                loadingContainer.style.display = 'none';
                loadingBar.style.width = '0%';
                currentLoadingPercentage = 0;
            }, 200);
        });
    }
};

// ── File input ────────────────────────────────────────────────────────────────
document.getElementById('fileInput').addEventListener('change', (event) => {
    const file = event.target.files[0];
    if (!file) return;

    loadingContainer.style.display = 'block';
    incrementLoadingBar(5);

    const reader = new FileReader();
    reader.onprogress = function (e) {
        if (e.lengthComputable) {
            loadingBar.style.width = (currentLoadingPercentage + (e.loaded / e.total) * FileLoadPercentage) + '%';
        }
    };
    reader.onload = function (e) {
        setLoadingBar(15);
        setTimeout(() => {
            const data = JSON.parse(e.target.result);
            voxelData = data;
            incrementLoadingBar(JsonParsePercentage);
            if (mesh) scene.remove(mesh);
            setLoadingBar(50);
            worker.postMessage(data);
        }, 50);
    };
    reader.readAsText(file);
});

// ── Resize ────────────────────────────────────────────────────────────────────
window.addEventListener('resize', () => {
    sizes.width  = window.innerWidth;
    sizes.height = window.innerHeight;
    camera.aspect = sizes.width / sizes.height;
    camera.updateProjectionMatrix();
    renderer.setSize(sizes.width, sizes.height);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
});

// ── Renderer ──────────────────────────────────────────────────────────────────
const renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
renderer.setSize(sizes.width, sizes.height);
renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
renderer.outputEncoding = THREE.sRGBEncoding;

// ── Render loop ───────────────────────────────────────────────────────────────
const tick = () => {
	const direction = new THREE.Vector3();
	camera.getWorldDirection(direction);
	direction.y = 0;
	direction.normalize();

	const right = new THREE.Vector3();
	right.crossVectors(direction, new THREE.Vector3(0, 1, 0)).normalize();

	if (keys["KeyW"]) {
		camera.position.add(direction.clone().multiplyScalar(moveSpeed));
		controls.target.add(direction.clone().multiplyScalar(moveSpeed));
	}
	if (keys["KeyS"]) {
		camera.position.add(direction.clone().multiplyScalar(-moveSpeed));
		controls.target.add(direction.clone().multiplyScalar(-moveSpeed));
	}
	if (keys["KeyA"]) {
		camera.position.add(right.clone().multiplyScalar(-moveSpeed));
		controls.target.add(right.clone().multiplyScalar(-moveSpeed));
	}
	if (keys["KeyD"]) {
		camera.position.add(right.clone().multiplyScalar(moveSpeed));
		controls.target.add(right.clone().multiplyScalar(moveSpeed));
	}

    controls.update();
    updateTooltip();
    renderer.render(scene, camera);
    requestAnimationFrame(tick);
};
tick();