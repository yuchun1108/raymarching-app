precision mediump float;

uniform vec2 u_resolution;
uniform float u_fov;
const int MAX_MARCHING_STEPS=255;
const float MIN_DIST=0.;
const float MAX_DIST=100.;
const float EPSILON=.0001;

float sphereSDF1(vec3 samplePoint){
  return length(samplePoint-vec3(.05,0,0))-.5;
}

float sphereSDF2(vec3 samplePoint){
  return length(samplePoint-vec3(-.05,0,0))-.5;
}

float sceneSDF(vec3 samplePoint){
  float dist=min(sphereSDF1(samplePoint),sphereSDF2(samplePoint));
  return dist;
}

float shortestDistanceToSurface(vec3 eye,vec3 marchingDirection,float start,float end){
  float depth=start;
  for(int i=0;i<MAX_MARCHING_STEPS;i++){
    float dist=sceneSDF(eye+depth*marchingDirection);
    if(dist<EPSILON){
      return depth;
    }
    depth+=dist;
    if(depth>=end){
      return end;
    }
  }
  return end;
}

vec3 estimateNormal(vec3 p){
  return normalize(vec3(
      sceneSDF(vec3(p.x+EPSILON,p.y,p.z))-sceneSDF(vec3(p.x-EPSILON,p.y,p.z)),
      sceneSDF(vec3(p.x,p.y+EPSILON,p.z))-sceneSDF(vec3(p.x,p.y-EPSILON,p.z)),
      sceneSDF(vec3(p.x,p.y,p.z+EPSILON))-sceneSDF(vec3(p.x,p.y,p.z-EPSILON))
    ));
  }
  
  vec3 rayDirection(float fieldOfView,vec2 size,vec2 fragCoord){
    vec2 xy=fragCoord-size/2.;
    float z=size.y/tan(radians(fieldOfView)/2.);
    return normalize(vec3(xy,-z));
  }
  
  /**
  * Lighting contribution of a single point light source via Phong illumination.
  *
  * The vec3 returned is the RGB color of the light's contribution.
  *
  * k_a: Ambient color
  * k_d: Diffuse color
  * k_s: Specular color
  * alpha: Shininess coefficient
  * p: position of point being lit
  * eye: the position of the camera
  * lightPos: the position of the light
  * lightIntensity: color/intensity of the light
  *
  * See https://en.wikipedia.org/wiki/Phong_reflection_model#Description
  */
  vec3 phongContribForLight(vec3 k_d, vec3 k_s, float alpha, vec3 p, vec3 eye,
  vec3 lightPos, vec3 lightIntensity) {
    vec3 N = estimateNormal(p);
    vec3 L = normalize(lightPos - p);
    vec3 V = normalize(eye - p);
    vec3 R = normalize(reflect(-L, N));
    
    float dotLN = dot(L, N);
    float dotRV = dot(R, V);
    
    if (dotLN < 0.0) {
      // Light not visible from this point on the surface
      return vec3(0.0, 0.0, 0.0);
    }
    
    if (dotRV < 0.0) {
      // Light reflection in opposite direction as viewer, apply only diffuse
      // component
      return lightIntensity * (k_d * dotLN);
    }
    return lightIntensity * (k_d * dotLN + k_s * pow(dotRV, alpha));
  }
  
  /**
  * Lighting via Phong illumination.
  *
  * The vec3 returned is the RGB color of that point after lighting is applied.
  * k_a: Ambient color
  * k_d: Diffuse color
  * k_s: Specular color
  * alpha: Shininess coefficient
  * p: position of point being lit
  * eye: the position of the camera
  *
  * See https://en.wikipedia.org/wiki/Phong_reflection_model#Description
  */
  vec3 phongIllumination(vec3 k_a,vec3 k_d,vec3 k_s,float alpha,vec3 p,vec3 eye){
    const vec3 ambientLight=.5*vec3(1.,1.,1.);
    vec3 color=ambientLight*k_a;
    
    vec3 light1Pos=vec3(4.,2.,4.);
    vec3 light1Intensity=vec3(.4,.4,.4);
    
    color+=phongContribForLight(k_d,k_s,alpha,p,eye,light1Pos,light1Intensity);
    
    return color;
  }
  
  void main(){
    vec3 dir=rayDirection(u_fov,u_resolution,gl_FragCoord.xy);
    vec3 eye=vec3(0.,0.,5.);
    float dist=shortestDistanceToSurface(eye,dir,MIN_DIST,MAX_DIST);
    
    if(dist>MAX_DIST-EPSILON){
      gl_FragColor=vec4(0.,0.,0.,0.);
      return;
    }
    
    vec3 p=eye+dist*dir;
    // vec3 nor=estimateNormal(p);
    // vec2 st=gl_FragCoord.xy/u_resolution.xy;
    // st.x *= u_resolution.x / u_resolution.y;
    
    // vec3 color=vec3(0.);
    // color=vec3(st.x,st.y,abs(sin(u_time)));
    
    // nor=nor*.5+vec3(.5,.5,.5);
    
    vec3 K_a=vec3(.2,.2,.2);
    vec3 K_d=vec3(.7,.2,.2);
    vec3 K_s=vec3(1.,1.,1.);
    float shininess=10.;
    
    vec3 color=phongIllumination(K_a,K_d,K_s,shininess,p,eye);
    
    gl_FragColor=vec4(color,1.);
  }