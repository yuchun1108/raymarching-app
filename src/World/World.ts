import seedrandom from "seedrandom";
import * as THREE from "three";
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls.js";
import { GUI } from "three/examples/jsm/libs/lil-gui.module.min.js";
import Stats from "three/examples/jsm/libs/stats.module.js";

export default class World {
  private readonly fov = 45;
  private readonly max_ball_count = 16;
  private readonly def_ball_count = 8;
  private readonly def_dot_size = 0.3;
  private readonly def_smooth_value = 0.8;

  renderer: THREE.WebGLRenderer;
  camera: THREE.PerspectiveCamera;
  scene: THREE.Scene;
  light: THREE.Light;
  material: THREE.RawShaderMaterial | undefined;
  plane: THREE.Mesh | undefined;
  balls: THREE.Mesh[] = [];
  ballYArr: number[] = [];
  stats: Stats;
  settings: any;

  private vs: string | undefined;
  private fs: string | undefined;

  constructor() {
    this.renderer = new THREE.WebGLRenderer();
    this.renderer.setPixelRatio(window.devicePixelRatio);
    this.renderer.setSize(window.innerWidth, window.innerHeight);
    document.body.appendChild(this.renderer.domElement);

    this.camera = new THREE.PerspectiveCamera(this.fov, 1, 1, 1000);
    this.camera.position.x = 10;
    this.camera.position.z = 10;

    console.log(this.camera.matrixWorld);
    console.log(this.camera.projectionMatrixInverse.clone());

    this.scene = new THREE.Scene();

    const ambLight = new THREE.AmbientLight(0x404040); // soft white light
    this.scene.add(ambLight);

    this.light = new THREE.PointLight("yellow", 500, 1000);
    this.light.position.set(20, 10, 15);
    this.scene.add(this.light);

    this.createBalls();
    this.loadShader();

    this.onWindowResize();
    window.addEventListener("resize", this.onWindowResize.bind(this));

    const controls = new OrbitControls(this.camera, this.renderer.domElement);
    // controls.enableZoom = false;
    controls.update();

    // this.renderer.render(this.scene, this.camera);

    this.renderer.setAnimationLoop(this.update.bind(this));

    this.stats = new Stats();
    document.body.appendChild(this.stats.dom);

    this.createPanel();
  }

  createBalls() {
    //const ballPosArr =
    const mat = new THREE.MeshStandardMaterial({ color: 0x049ef4 });

    const random = seedrandom("a");
    console.log(random());

    const posScale = 4;
    const sizeScale = 1;

    for (let i = 0; i < this.max_ball_count; i++) {
      const ball = new THREE.Mesh(new THREE.SphereGeometry(), mat);
      ball.position.set(
        (random() - 0.5) * posScale,
        (random() - 0.5) * posScale,
        (random() - 0.5) * posScale
      );
      const size = ((random() - 0.5) * 1 + 1) * sizeScale;
      ball.scale.set(size, size, size);

      this.scene.add(ball);
      this.balls.push(ball);
      this.ballYArr.push(ball.position.y);
    }
  }

  loadShader() {
    fetch("shader/myShader.vs")
      .then((response) => {
        return response.text();
      })
      .then((text) => {
        this.vs = text;
        this.setupShaderMaterial();
      })
      .catch((err) => {
        console.error(err);
      });

    fetch("shader/myShader.fs")
      .then((response) => {
        return response.text();
      })
      .then((text) => {
        this.fs = "#define THREE_JS 1\n" + text;
        this.setupShaderMaterial();
      })
      .catch((err) => {
        console.error(err);
      });
  }

  setupShaderMaterial() {
    if (this.vs === undefined || this.fs === undefined) return;

    const ballVec4Arr = new Array<THREE.Vector4>(this.max_ball_count);
    for (let i = 0; i < ballVec4Arr.length; i++) {
      ballVec4Arr[i] = new THREE.Vector4(0, 0, 0, 1);
    }

    this.material = new THREE.RawShaderMaterial({
      uniforms: {
        u_resolution: {
          value: new THREE.Vector2(window.innerWidth, window.innerHeight),
        },
        u_fov: { value: this.fov },
        u_cameraWorldMatrix: { value: this.camera.matrixWorld },
        u_cameraProjectionMatrixInverse: {
          value: this.camera.projectionMatrixInverse.clone(),
        },
        u_cameraPosition: { value: this.camera.position.clone() },
        u_ballCount: { value: this.def_ball_count },
        u_balls: {
          value: ballVec4Arr,
        },
        u_dotSize: { value: this.def_dot_size },
        u_smoothValue: { value: this.def_smooth_value },
      },
      vertexShader: this.vs,
      fragmentShader: this.fs,
    });

    this.plane = new THREE.Mesh(new THREE.PlaneGeometry(), this.material);
    this.scene.add(this.plane);

    this.vs = this.fs = undefined;
  }

  createPanel() {
    const panel = new GUI({ width: 310 });

    this.settings = {
      "ray marching": true,
      "ball count": this.def_ball_count,
      "dot size": this.def_dot_size,
      "smooth value": this.def_smooth_value,
    };

    panel.add(this.settings, "ray marching").onChange((value) => {
      if (this.plane) this.plane.visible = value;
    });

    panel.add(this.settings, "ball count", 1, 16, 1).onChange((value) => {
      for (let i = 0; i < this.max_ball_count; i++) {
        this.balls[i].visible = i < value;
      }

      if (this.material) this.material.uniforms.u_ballCount.value = value;
    });

    panel.add(this.settings, "dot size", 0, 1, 0.01).onChange((value) => {
      if (this.material) this.material.uniforms.u_dotSize.value = value;
    });

    panel.add(this.settings, "smooth value", 0, 1, 0.01).onChange((value) => {
      if (this.material) this.material.uniforms.u_smoothValue.value = value;
    });
  }

  // onRayMarchingChange(value: boolean) {}

  // onBallCountChan;

  onWindowResize() {
    const width = window.innerWidth;
    const height = window.innerHeight;

    this.renderer.setSize(width, height);

    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();

    this.renderer.setPixelRatio(window.devicePixelRatio);
    this.renderer.setSize(window.innerWidth, window.innerHeight);

    this.material?.uniforms.u_resolution.value.set(width, height);
  }

  update(time: number) {
    time *= 0.001;

    for (let i = 0; i < this.balls.length; i++) {
      const ball = this.balls[i];
      let ballY = this.ballYArr[i];
      ballY = Math.sin(ballY + time * (ballY * 0.3 + 0.2)) * 3;
      ball.position.y = ballY;
    }

    if (this.material) {
      const uniforms = this.material.uniforms;
      uniforms.u_cameraProjectionMatrixInverse.value.copy(
        this.camera.projectionMatrixInverse
      );
      uniforms.u_cameraPosition.value.copy(this.camera.position);

      const count = Math.min(this.balls.length, this.max_ball_count);
      for (let i = 0; i < count; i++) {
        const ball = this.balls[i];
        uniforms.u_balls.value[i].x = ball.position.x;
        uniforms.u_balls.value[i].y = ball.position.y;
        uniforms.u_balls.value[i].z = ball.position.z;
        uniforms.u_balls.value[i].w = ball.scale.x;
      }
    }

    this.renderer.render(this.scene, this.camera);

    this.stats.update();
  }
}
