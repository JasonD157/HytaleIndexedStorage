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
// Disable right-click pan
controls.enablePan = false;
controls.mouseButtons.RIGHT = null;

let moveCamTo = function (x, y, z) {
	const target = new THREE.Vector3(x + 0.5, y + 0.5, z + 0.5);

    // Keep same camera offset distance
    const offset = camera.position.clone().sub(controls.target);

    controls.target.copy(target);
    camera.position.copy(target.clone().add(offset));
}

// Keyboard movement
const keys = {};
const moveSpeed = 0.8;

document.addEventListener("keydown", (e) => {
    keys[e.code] = true;
});

document.addEventListener("keyup", (e) => {
    keys[e.code] = false;
});

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
const worker = new Worker('./meshWorker.js');
let currentLoadingPercentage = 0
const FileLoadPercentage = 10
const JsonParsePercentage = 30
const VoxelParsePercentage = 30
let incrementLoadingBar = function (percent) {
	currentLoadingPercentage += percent
	loadingBar.style.width = currentLoadingPercentage + "%"
}
let setLoadingBar = function (percent) {
	currentLoadingPercentage = percent
	loadingBar.style.width = currentLoadingPercentage + "%"
}

worker.onmessage = function(e) {

	if (e.data.progress !== undefined) {
		loadingBar.style.width = (currentLoadingPercentage + e.data.progress * VoxelParsePercentage) + "%";
		// Inc till 80%
    }

	if (e.data.done) {
		setLoadingBar(80)
        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute(
            'position',
            new THREE.Float32BufferAttribute(e.data.positions, 3)
        );
        geometry.setAttribute(
            'color',
            new THREE.Float32BufferAttribute(e.data.colors, 3)
		);
		setLoadingBar(85)
		geometry.setIndex(e.data.indices);
		geometry.computeVertexNormals();
		setLoadingBar(90)

        const material = new THREE.MeshLambertMaterial({ vertexColors:true });
		mesh = new THREE.Mesh(geometry, material);
		setLoadingBar(95)
		scene.add(mesh);

		moveCamTo(e.data.x, e.data.y, e.data.z)

		requestAnimationFrame(() => {
			renderer.render(scene, camera);

			setLoadingBar(100);

			setTimeout(() => {
				loadingContainer.style.display = "none";
				loadingBar.style.width = "0%";
				currentLoadingPercentage = 0;
			}, 200);
		});

        setTimeout(() => {
            loadingContainer.style.display = "none";
            loadingBar.style.width = "0%";
        }, 300);
    }
};

// File input
document.getElementById('fileInput').addEventListener('change', (event) => {
    const file = event.target.files[0];
    if (!file) return;

    const loadingContainer = document.getElementById("loadingContainer");
    const loadingBar = document.getElementById("loadingBar");

	loadingContainer.style.display = "block";

	const reader = new FileReader();

	incrementLoadingBar(5)

    reader.onprogress = function(e) {
        if (e.lengthComputable) {
            const percent = (e.loaded / e.total) * FileLoadPercentage;
			let temp = currentLoadingPercentage + percent
			loadingBar.style.width = temp + "%";
        }
	};
	

    reader.onload = function(e) {
		setLoadingBar(15)

        setTimeout(() => {
			const data = JSON.parse(e.target.result);
			incrementLoadingBar(JsonParsePercentage)

            if(mesh) scene.remove(mesh);

			setLoadingBar(50)

            worker.postMessage(data);

            setTimeout(() => {
                loadingContainer.style.display = "none";
				loadingBar.style.width = "0%";
            }, 300);

        }, 50);
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

document.getElementById("goto").onclick = function () {
    const x = parseInt(document.getElementById("x").value);
    const y = parseInt(document.getElementById("y").value);
    const z = parseInt(document.getElementById("z").value);

	moveCamTo(x,y,z)
};

// Animation
const tick = () => {
	const direction = new THREE.Vector3();
	camera.getWorldDirection(direction);

	const right = new THREE.Vector3();
	right.crossVectors(direction, camera.up).normalize();

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
	if (keys["KeyE"]) {
		camera.position.y += moveSpeed;
		controls.target.y += moveSpeed;
	}
	if (keys["KeyQ"]) {
		camera.position.y -= moveSpeed;
		controls.target.y -= moveSpeed;
	}
    controls.update()
    renderer.render(scene, camera)
    window.requestAnimationFrame(tick)
}

tick()
