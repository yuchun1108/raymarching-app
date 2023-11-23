precision mediump float;

const int MAX_MARCHING_STEPS=255;
const float MIN_DIST=0.;
const float MAX_DIST=100.;
const float EPSILON=.001;
const int MAX_BALL_COUNT=16;

uniform float u_fov;
uniform vec2 u_resolution;
#if defined(THREE_JS)
uniform vec4 u_balls[MAX_BALL_COUNT];
uniform int u_ballCount;
#else
vec4 u_balls[MAX_BALL_COUNT];
int u_ballCount;
#endif
uniform mat4 u_cameraWorldMatrix;
uniform mat4 u_cameraProjectionMatrixInverse;
uniform vec3 u_cameraPosition;
uniform float u_dotSize;
uniform float u_smoothValue;

float sphereSDF(vec3 samplePoint,vec3 center,float size){
  return length(samplePoint-center)-size;
}

float opSmoothUnion(float d1,float d2,float k)
{
  float h=clamp(.5+.5*(d2-d1)/k,0.,1.);
  return mix(d2,d1,h)-k*h*(1.-h);
}

float sceneSDF(vec3 samplePoint){
  float dist=MAX_DIST;
  for(int i=0;i<MAX_BALL_COUNT;i++)
  {
    if(i>=u_ballCount)break;
    dist=opSmoothUnion(sphereSDF(samplePoint,u_balls[i].xyz,u_balls[i].w),dist,u_smoothValue);
  }
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
    )
  );
}

vec3 phongContribForLight(vec3 k_a,vec3 k_d,vec3 k_s,float alpha,vec3 p,vec3 eye,
vec3 lightPos,vec3 lightIntensity){
  vec3 N=estimateNormal(p);
  vec3 L=normalize(lightPos-p);
  vec3 V=normalize(eye-p);
  vec3 R=normalize(reflect(-L,N));
  
  float dotLN=dot(L,N);
  float dotRV=dot(R,V);
  
  if(dotLN<0.){
    // Light not visible from this point on the surface
    return vec3(0.,0.,0.);
  }
  
  if(dotRV<0.){
    // Light reflection in opposite direction as viewer, apply only diffuse
    // component
    return lightIntensity*(k_d*dotLN);
  }
  return lightIntensity*(k_d*dotLN+k_s*pow(dotRV,alpha));
}

vec3 illumination(vec3 k_a,vec3 k_d,vec3 k_s,float alpha,vec3 p,vec3 eye){
  const vec3 ambientLight=.5*vec3(1.,1.,1.);
  vec3 color=ambientLight*k_a*k_d;
  
  vec3 light1Pos=vec3(4.,2.,4.);
  vec3 light1Intensity=vec3(.4,.4,.4);
  
  color+=phongContribForLight(k_a,k_d,k_s,alpha,p,eye,light1Pos,light1Intensity);
  
  return color;
}

vec3 rayDirection(float fieldOfView,vec2 size,vec2 fragCoord){
  vec2 xy=fragCoord-size/2.;
  float z=size.y/tan(radians(fieldOfView)/2.);
  return normalize(vec3(xy,-z));
}

void main(){
  
  #if defined(THREE_JS)
  // screen position
  vec2 screenPos=(gl_FragCoord.xy*2.-u_resolution)/u_resolution;
  
  // ray direction in normalized device coordinate
  vec4 ndcRay=vec4(screenPos.xy,1.,1.);
  
  // convert ray direction from normalized device coordinate to world coordinate
  vec3 ray=(u_cameraWorldMatrix*u_cameraProjectionMatrixInverse*ndcRay).xyz;
  ray=normalize(ray);
  #else
  u_ballCount=2;
  u_balls[0]=vec4(0,0,0,1.);
  u_balls[1]=vec4(1.5,0,0,.5);
  
  vec3 ray=rayDirection(u_fov,u_resolution,gl_FragCoord.xy);
  
  #endif
  gl_FragColor=vec4(ray,1);
  // return;
  
  vec3 eye=u_cameraPosition;
  float dist=shortestDistanceToSurface(eye,ray,MIN_DIST,MAX_DIST);
  
  if(dist>MAX_DIST-EPSILON){
    gl_FragColor=vec4(0.,0.,0.,1.);
    return;
  }
  
  vec3 p=eye+dist*ray;
  bool isDot=length(mod(p,1.)-.5)<u_dotSize;
  
  vec3 K_a=vec3(.2,.2,.2);
  vec3 K_d=isDot?vec3(1.6,1.2,1.):vec3(.8118,.0902,.0902);
  vec3 K_s=vec3(1.,1.,1.);
  float shininess=isDot?1.:2.;
  
  vec3 color=illumination(K_a,K_d,K_s,shininess,p,eye);
  
  gl_FragColor=vec4(color,1.);
}