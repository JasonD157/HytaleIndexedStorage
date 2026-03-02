// raycastWorker.js
// Init:  { type:'init', positions:Float32Array, indices:Uint32Array }
// Query: { type:'cast', id, ox,oy,oz, dx,dy,dz }
// Reply: { id, hit:true, px,py,pz, nx,ny,nz }  OR  { id, hit:false }

let positions = null;
let indices   = null;

// Möller–Trumbore — returns t or -1
function intersectTri(ox,oy,oz, dx,dy,dz, ax,ay,az, bx,by,bz, cx,cy,cz) {
    const EPS = 1e-7;
    const e1x=bx-ax, e1y=by-ay, e1z=bz-az;
    const e2x=cx-ax, e2y=cy-ay, e2z=cz-az;
    const hx=dy*e2z-dz*e2y, hy=dz*e2x-dx*e2z, hz=dx*e2y-dy*e2x;
    const a=e1x*hx+e1y*hy+e1z*hz;
    if (a>-EPS && a<EPS) return -1;
    const f=1/a;
    const sx=ox-ax, sy=oy-ay, sz=oz-az;
    const u=f*(sx*hx+sy*hy+sz*hz);
    if (u<0||u>1) return -1;
    const qx=sy*e1z-sz*e1y, qy=sz*e1x-sx*e1z, qz=sx*e1y-sy*e1x;
    const v=f*(dx*qx+dy*qy+dz*qz);
    if (v<0||u+v>1) return -1;
    const t=f*(e2x*qx+e2y*qy+e2z*qz);
    return t>EPS ? t : -1;
}

self.onmessage = function(e) {
    const msg = e.data;

    if (msg.type === 'init') {
        positions = msg.positions;
        indices   = msg.indices;
        return;
    }

    if (msg.type === 'cast') {
        if (!positions || !indices) { self.postMessage({ id: msg.id, hit: false }); return; }

        const { id, ox,oy,oz, dx,dy,dz } = msg;

        let bestT  = Infinity;
        let bestTri = -1;

        const triCount = indices.length / 3;
        for (let t = 0; t < triCount; t++) {
            const i0=indices[t*3+0]*3, i1=indices[t*3+1]*3, i2=indices[t*3+2]*3;
            const hit = intersectTri(
                ox,oy,oz, dx,dy,dz,
                positions[i0],positions[i0+1],positions[i0+2],
                positions[i1],positions[i1+1],positions[i1+2],
                positions[i2],positions[i2+1],positions[i2+2]
            );
            if (hit>0 && hit<bestT) { bestT=hit; bestTri=t; }
        }

        if (bestTri === -1) { self.postMessage({ id, hit: false }); return; }

        // Hit point
        const px=ox+dx*bestT, py=oy+dy*bestT, pz=oz+dz*bestT;

        // Face normal = cross(e1, e2), normalised
        const i0=indices[bestTri*3+0]*3, i1=indices[bestTri*3+1]*3, i2=indices[bestTri*3+2]*3;
        const ax=positions[i0],ay=positions[i0+1],az=positions[i0+2];
        const e1x=positions[i1]-ax,   e1y=positions[i1+1]-ay, e1z=positions[i1+2]-az;
        const e2x=positions[i2]-ax,   e2y=positions[i2+1]-ay, e2z=positions[i2+2]-az;
        let nx=e1y*e2z-e1z*e2y, ny=e1z*e2x-e1x*e2z, nz=e1x*e2y-e1y*e2x;
        const nl=Math.sqrt(nx*nx+ny*ny+nz*nz);
        nx/=nl; ny/=nl; nz/=nl;
        // Ensure normal faces toward ray origin
        if (nx*dx+ny*dy+nz*dz > 0) { nx=-nx; ny=-ny; nz=-nz; }

        self.postMessage({ id, hit:true, px,py,pz, nx,ny,nz });
    }
};