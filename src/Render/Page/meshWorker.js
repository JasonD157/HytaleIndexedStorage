let tofloats = function (hex) {
	hex = hex.replace(/^#/, '');
	const r = parseInt(hex.substring(0, 2), 16) / 255;
	const g = parseInt(hex.substring(2, 4), 16) / 255;
	const b = parseInt(hex.substring(4, 6), 16) / 255;

	return [r, g, b];
}

self.onmessage = function (e) {
    const voxels = e.data;
    const voxelKeys = Object.keys(voxels)
        .map(k => k.split(',').map(Number));

    const total = voxelKeys.length;

    const positions = [];
    const colors = [];
    const indices = [];
    let indexOffset = 0;

    const directions = [
        [1,0,0], [-1,0,0],
        [0,1,0], [0,-1,0],
        [0,0,1], [0,0,-1]
    ];

    const faceVertices = [
        [[1,0,0],[1,1,0],[1,1,1],[1,0,1]],
        [[0,0,1],[0,1,1],[0,1,0],[0,0,0]],
        [[0,1,1],[1,1,1],[1,1,0],[0,1,0]],
        [[0,0,0],[1,0,0],[1,0,1],[0,0,1]],
        [[0,0,1],[1,0,1],[1,1,1],[0,1,1]],
        [[0,1,0],[1,1,0],[1,0,0],[0,0,0]]
    ];

    const voxelSet = new Set(Object.keys(voxels));

    for (let v = 0; v < total; v++) {
        const [x,y,z] = voxelKeys[v];
        const color = tofloats(voxels[`${x},${y},${z}`]);

        for (let f = 0; f < 6; f++) {
            const nx = x + directions[f][0];
            const ny = y + directions[f][1];
            const nz = z + directions[f][2];

            if (voxelSet.has(`${nx},${ny},${nz}`)) continue;

            for (let i = 0; i < 4; i++) {
                const vert = faceVertices[f][i];
                positions.push(x+vert[0], y+vert[1], z+vert[2]);
                colors.push(...color);
            }

            indices.push(
                indexOffset, indexOffset+1, indexOffset+2,
                indexOffset, indexOffset+2, indexOffset+3
            );
            indexOffset += 4;
        }

        // Send progress every 2%
        if (v % Math.floor(total / 50) === 0) {
            self.postMessage({ progress: (v / total) });
        }
    }

	// Get a block to set the cam to
	let [x, y, z] = voxelKeys[0];

    self.postMessage({
        done: true,
        positions,
        colors,
		indices,
		x,
		y,
		z
    });
};
