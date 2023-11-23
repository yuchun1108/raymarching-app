export const vertex_shader = `
  attribute vec3 position;
  void main(){
    gl_Position = vec4(position*2., 1.0);
  }
`;

export const fragment_shader = `
precision mediump float;

const int MAX_MARCHING_STEPS=255;
const float MIN_DIST=0.;
const float MAX_DIST=100.;
const float EPSILON=.0001;

uniform float fov;
uniform vec2 resolution;

vec3 rayDirection(float fieldOfView,vec2 size,vec2 fragCoord){
  vec2 xy=fragCoord-size/2.;
  float z=size.y/tan(radians(fieldOfView)/2.);
  return normalize(vec3(xy,-z));
}

void main(){
  vec2 screenPos = ( gl_FragCoord.xy * 2.0 - resolution ) / resolution;
  gl_FragColor=vec4(screenPos.xy,0,1.);
}
`;
