using UnityEngine;
using System.Collections;

public class MeshUtilities:MonoBehaviour{
	public void ProjectionMesh(Transform meshTransform,Mesh mesh,Ray ray,float radius,Material material){
		//Debug Code Start
		float startTime=Time.realtimeSinceStartup;
		//Debug Code Stop
		Vector3[] meshVertices=mesh.vertices;
		int newVertices=0;
		int[] newTriangles=new int[meshVertices.Length];
		for(int i=0,j=0;i<meshVertices.Length;i+=3){
			Vector3 v0=meshTransform.TransformPoint(meshVertices[i]);
			Vector3 v1=meshTransform.TransformPoint(meshVertices[i+1]);
			Vector3 v2=meshTransform.TransformPoint(meshVertices[i+2]);
			if(RayTriangleIntersection(ray,v0,v1,v2,radius)){
				newTriangles[j]=i;
				newTriangles[j+1]=i+1;
				newTriangles[j+2]=i+2;
				j+=3;
			}
			newVertices=j;
		}
		Vector3[] vertices=new Vector3[newVertices];
		Vector2[] uv=new Vector2[newVertices];
		int[] triangles=new int[newVertices];
		Vector3 x=Vector3.Cross(ray.direction,meshVertices[0]).normalized;
		Vector3 y=Vector3.Cross(ray.direction,x).normalized;
		Vector2 offset=new Vector2(Vector3.Dot(ray.origin,-x),Vector3.Dot(ray.origin,-y))*0.5f/radius+Vector2.one*0.5f;
		for(int i=0;i<vertices.Length;i++){
			vertices[i]=meshVertices[newTriangles[i]];
			Vector3 v=meshTransform.TransformPoint(vertices[i]);
			uv[i]=new Vector2(Vector3.Dot(v,x),Vector3.Dot(v,y))*0.5f/radius+offset;
			triangles[i]=i;
		}
		Mesh newMesh=new Mesh();
		newMesh.Clear();
		newMesh.vertices=vertices;
		newMesh.uv=uv;
		newMesh.normals=new Vector3[triangles.Length];
		newMesh.triangles=triangles;
		newMesh.RecalculateNormals();
		GameObject newGameObject=new GameObject();
		newGameObject.AddComponent<MeshFilter>();
		newGameObject.AddComponent<MeshRenderer>();
		newGameObject.GetComponent<MeshFilter>().mesh=newMesh;
		newGameObject.GetComponent<MeshRenderer>().material=material;
		newGameObject.transform.name=material.name+" Projection";
		newGameObject.transform.parent=meshTransform;
		newGameObject.transform.localPosition=Vector3.zero;
		newGameObject.transform.localRotation=Quaternion.identity;
		newGameObject.transform.localScale=Vector3.one;
		//Debug Code Start
		print("ProjectionMesh total execution time: "+(Time.realtimeSinceStartup-startTime)+" seconds for "+(newMesh.triangles.Length/3)+" triangles");
		//Debug Code Stop
	}
	
	public void PunctureMesh(Transform meshTransform,Mesh mesh,Ray ray,float radius,int planes){
		//Debug Code Start
		float startTime=Time.realtimeSinceStartup;
		//Debug Code Stop
		Vector3[] meshVertices=mesh.vertices;
		Vector2[] meshUV=mesh.uv;
		int newVertices=0;
		int oldVertices=0;
		int[] newTriangles=new int[meshVertices.Length];
		int[] oldTriangles=new int[meshVertices.Length];
		for(int i=0,j=0,k=0;i<meshVertices.Length;i+=3){
			Vector3 v0=meshTransform.TransformPoint(meshVertices[i]);
			Vector3 v1=meshTransform.TransformPoint(meshVertices[i+1]);
			Vector3 v2=meshTransform.TransformPoint(meshVertices[i+2]);
			if(RayTriangleIntersection(ray,v0,v1,v2,radius)){
				newTriangles[j]=i;
				newTriangles[j+1]=i+1;
				newTriangles[j+2]=i+2;
				j+=3;
			}
			else{
				oldTriangles[k]=i;
				oldTriangles[k+1]=i+1;
				oldTriangles[k+2]=i+2;
				k+=3;
			}
			newVertices=j;
			oldVertices=k;
		}
		Vector3[] vertices=new Vector3[newVertices];
		Vector2[] uv=new Vector2[newVertices];
		int[] triangles=new int[newVertices];
		for(int i=0;i<triangles.Length;i++){
			vertices[i]=meshVertices[newTriangles[i]];
			uv[i]=meshUV[newTriangles[i]];
			triangles[i]=i;
		}
		if(triangles.Length==0){
			return;
		}
		CombineInstance[] combineInstance=new CombineInstance[planes+1];
		for(int i=0;i<planes;i++){
			Mesh newMesh=new Mesh();
			newMesh.Clear();
			newMesh.vertices=vertices;
			newMesh.uv=uv;
			newMesh.normals=new Vector3[triangles.Length];
			newMesh.triangles=triangles;
			newMesh.RecalculateNormals();
			Vector3 s0=Vector3.Cross(ray.direction,new Vector3(Mathf.Pow(ray.direction.x,2)+1,Mathf.Pow(ray.direction.y,2)+2,Mathf.Pow(ray.direction.z,2)+3));
			s0=(Quaternion.AngleAxis(i*360/planes,ray.direction)*s0.normalized);
			SplitMesh(meshTransform,newMesh,new Plane(-s0,ray.origin-s0*radius),false);
			combineInstance[i].mesh=newMesh;
		}
		vertices=new Vector3[oldVertices];
		uv=new Vector2[oldVertices];
		triangles=new int[oldVertices];
		for(int i=0;i<vertices.Length;i++){
			vertices[i]=meshVertices[oldTriangles[i]];
			uv[i]=meshUV[oldTriangles[i]];
			triangles[i]=i;
		}
		Mesh oldMesh=new Mesh();
		oldMesh.Clear();
		oldMesh.vertices=vertices;
		oldMesh.uv=uv;
		oldMesh.normals=new Vector3[triangles.Length];
		oldMesh.triangles=triangles;
		oldMesh.RecalculateNormals();
		combineInstance[planes].mesh=oldMesh;
		mesh.Clear();
		mesh.CombineMeshes(combineInstance,true,false);
		mesh.RecalculateNormals();
		//Debug Code Start
		print("PunctureMesh total execution time: "+(Time.realtimeSinceStartup-startTime)+" seconds for "+(mesh.triangles.Length/3)+" triangles");
		//Debug Code Stop
	}
	
	bool RayTriangleIntersection(Ray ray,Vector3 v0,Vector3 v1,Vector3 v2,float r){
		Plane plane=new Plane(v0,v1,v2);
		Vector3 p0=Vector3.zero;
		float f0=-1;
		if(plane.Raycast(ray,out f0)){
			p0=ray.GetPoint(f0);
		}
		if(f0>0&&VertexTriangleIntersection(p0,v0,v1,v2)){
			return true;
		}
		else{
			if(RaySegmentIntersection(ray,v0,v1,r*2)){
				return true;
			}
			else if(RaySegmentIntersection(ray,v1,v2,r*2)){
				return true;
			}
			else if(RaySegmentIntersection(ray,v2,v0,r*2)){
				return true;
			}
		}
		return false;
	}
	
	bool VertexTriangleIntersection(Vector3 p0,Vector3 v0,Vector3 v1,Vector3 v2){
		if(Concave(p0,v0,v1,v2)&&Concave(p0,v1,v0,v2)&&Concave(p0,v2,v0,v1)){
			return true;
		}
		return false;
	}
	
	bool Concave(Vector3 v0,Vector3 v1,Vector3 v2,Vector3 v3){
		Vector3 cp0=Vector3.Cross(v3-v2,v0-v2);
		Vector3 cp1=Vector3.Cross(v3-v2,v1-v2);
		if(Vector3.Dot(cp0,cp1)>=0){
			return true;
		}
		return false;
	}
	
	bool RaySegmentIntersection(Ray ray,Vector3 v0,Vector3 v1,float d0){
		if(Vector3.Cross(ray.origin-v0,ray.direction).magnitude/ray.direction.magnitude<=d0){
			return true;
		}
		if(Vector3.Cross(ray.origin-v1,ray.direction).magnitude/ray.direction.magnitude<=d0){
			return true;
		}
		float f1=Vector3.Dot(ray.direction,ray.direction);
		float f2=Vector3.Dot(ray.direction,v1-v0);
		float f3=Vector3.Dot(v1-v0,v1-v0);
		float f4=Vector3.Dot(ray.direction,ray.origin-v0);
		float f5=Vector3.Dot(v1-v0,ray.origin-v0);
		if(f1*f3-f2*f2==0){
			Vector3 p0=(v1-v0)*f4/f2+v0;
			if(Vector3.Distance(ray.origin,p0)<=d0&&RayPointIntersection(new Ray(v0,v1-v0),p0,Vector3.Distance(v0,v1))){
				return true;
			}
		}
		else{
			Vector3 p0=ray.GetPoint((f2*f5-f3*f4)/(f1*f3-f2*f2));
			Vector3 p1=v0+(v1-v0)*(f1*f5-f2*f4)/(f1*f3-f2*f2);
			if(Vector3.Distance(p0,p1)<=d0&&RayPointIntersection(ray,p0,Mathf.Infinity)&&RayPointIntersection(new Ray(v0,v1-v0),p1,Vector3.Distance(v0,v1))){
				return true;
			}
		}
		return false;
	}
	
	bool RayPointIntersection(Ray ray,Vector3 v0,float d0){
		return (v0-ray.origin).magnitude<=d0&&(ray.origin+ray.direction*(v0-ray.origin).magnitude-v0).magnitude<0.001;
	}
	
	public void SplitMesh(Transform meshTransform,Mesh mesh,Plane plane,bool cap){
		//Debug Code Start
		float startTime=Time.realtimeSinceStartup;
		//Debug Code Stop
		Vector3[] meshVertices=mesh.vertices;
		Vector2[] meshUV=mesh.uv;
		int newTriangles=0;
		int newEdges=0;
		for(int i=0;i<meshVertices.Length;i+=3){
			float v0=plane.GetDistanceToPoint(meshTransform.TransformPoint(meshVertices[i]));
			float v1=plane.GetDistanceToPoint(meshTransform.TransformPoint(meshVertices[i+1]));
			float v2=plane.GetDistanceToPoint(meshTransform.TransformPoint(meshVertices[i+2]));
			v0=Mathf.Abs(v0)>0.001?v0:0;
			v1=Mathf.Abs(v1)>0.001?v1:0;
			v2=Mathf.Abs(v2)>0.001?v2:0;
			if(!(v0<=0&&v1<=0&&v2<=0)){
				newTriangles+=3;
				if(v0<0&&v1>0&&v2>0){
					newTriangles+=3;
				}
				else if(v0>0&&v1<0&&v2>0){
					newTriangles+=3;
				}
				else if(v0>0&&v1>0&&v2<0){
					newTriangles+=3;
				}
				if(!(v0>=0&&v1>=0&&v2>=0)){
					newEdges+=2;
				}
				else if(v0>0&&v1==0&&v2==0){
					newEdges+=2;
				}
				else if(v0==0&&v1>0&&v2==0){
					newEdges+=2;
				}
				else if(v0==0&&v1==0&&v2>0){
					newEdges+=2;
				}
			}
		}
		Vector3[] vertices=new Vector3[newTriangles+newEdges*3/2];
		Vector2[] uv=new Vector2[newTriangles+newEdges*3/2];
		int[] triangles=new int[newTriangles+newEdges*3/2];
		int[] edges=new int[newEdges];
		for(int i=0,j=0,k=0;i<meshVertices.Length;i+=3){
			float v0=plane.GetDistanceToPoint(meshTransform.TransformPoint(meshVertices[i]));
			float v1=plane.GetDistanceToPoint(meshTransform.TransformPoint(meshVertices[i+1]));
			float v2=plane.GetDistanceToPoint(meshTransform.TransformPoint(meshVertices[i+2]));
			v0=Mathf.Abs(v0)>0.001?v0:0;
			v1=Mathf.Abs(v1)>0.001?v1:0;
			v2=Mathf.Abs(v2)>0.001?v2:0;
			if(!(v0<=0&&v1<=0&&v2<=0)){
				if(v0>=0&&v1>=0&&v2>=0){
					vertices[j]=meshVertices[i];
					vertices[j+1]=meshVertices[i+1];
					vertices[j+2]=meshVertices[i+2];
					uv[j]=meshUV[i];
					uv[j+1]=meshUV[i+1];
					uv[j+2]=meshUV[i+2];
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					if(v0>0&&v1==0&&v2==0){
						edges[k]=j+1;
						edges[k+1]=j+2;
						k+=2;
					}
					else if(v0==0&&v1>0&&v2==0){
						edges[k]=j;
						edges[k+1]=j+2;
						k+=2;
					}
					else if(v0==0&&v1==0&&v2>0){
						edges[k]=j;
						edges[k+1]=j+1;
						k+=2;
					}
					j+=3;
				}
				else if(v0>0&&v1<0&&v2<0){
					vertices[j]=meshVertices[i];
					vertices[j+1]=meshVertices[i+1]+(meshVertices[i]-meshVertices[i+1])*(1-v0/(v0-v1));
					vertices[j+2]=meshVertices[i+2]+(meshVertices[i]-meshVertices[i+2])*(1-v0/(v0-v2));
					uv[j]=meshUV[i];
					uv[j+1]=meshUV[i+1]+(meshUV[i]-meshUV[i+1])*(1-v0/(v0-v1));
					uv[j+2]=meshUV[i+2]+(meshUV[i]-meshUV[i+2])*(1-v0/(v0-v2));
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					edges[k]=j+1;
					edges[k+1]=j+2;
					k+=2;
					j+=3;
				}
				else if(v0<0&&v1>0&&v2<0){
					vertices[j]=meshVertices[i]+(meshVertices[i+1]-meshVertices[i])*(1-v1/(v1-v0));
					vertices[j+1]=meshVertices[i+1];
					vertices[j+2]=meshVertices[i+2]+(meshVertices[i+1]-meshVertices[i+2])*(1-v1/(v1-v2));
					uv[j]=meshUV[i]+(meshUV[i+1]-meshUV[i])*(1-v1/(v1-v0));
					uv[j+1]=meshUV[i+1];
					uv[j+2]=meshUV[i+2]+(meshUV[i+1]-meshUV[i+2])*(1-v1/(v1-v2));
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					edges[k]=j;
					edges[k+1]=j+2;
					k+=2;
					j+=3;
				}
				else if(v0<0&&v1<0&&v2>0){
					vertices[j]=meshVertices[i]+(meshVertices[i+2]-meshVertices[i])*(1-v2/(v2-v0));
					vertices[j+1]=meshVertices[i+1]+(meshVertices[i+2]-meshVertices[i+1])*(1-v2/(v2-v1));
					vertices[j+2]=meshVertices[i+2];
					uv[j]=meshUV[i]+(meshUV[i+2]-meshUV[i])*(1-v2/(v2-v0));
					uv[j+1]=meshUV[i+1]+(meshUV[i+2]-meshUV[i+1])*(1-v2/(v2-v1));
					uv[j+2]=meshUV[i+2];
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					edges[k]=j;
					edges[k+1]=j+1;
					k+=2;
					j+=3;
				}
				else if(v0>0&&v1<=0&&v2<0){
					vertices[j]=meshVertices[i];
					vertices[j+1]=meshVertices[i+1]+(meshVertices[i]-meshVertices[i+1])*(1-v0/(v0-v1));
					vertices[j+2]=meshVertices[i+2]+(meshVertices[i]-meshVertices[i+2])*(1-v0/(v0-v2));
					uv[j]=meshUV[i];
					uv[j+1]=meshUV[i+1]+(meshUV[i]-meshUV[i+1])*(1-v0/(v0-v1));
					uv[j+2]=meshUV[i+2]+(meshUV[i]-meshUV[i+2])*(1-v0/(v0-v2));
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					edges[k]=j+1;
					edges[k+1]=j+2;
					k+=2;
					j+=3;
				}
				else if(v0<=0&&v1>0&&v2<0){
					vertices[j]=meshVertices[i]+(meshVertices[i+1]-meshVertices[i])*(1-v1/(v1-v0));
					vertices[j+1]=meshVertices[i+1];
					vertices[j+2]=meshVertices[i+2]+(meshVertices[i+1]-meshVertices[i+2])*(1-v1/(v1-v2));
					uv[j]=meshUV[i]+(meshUV[i+1]-meshUV[i])*(1-v1/(v1-v0));
					uv[j+1]=meshUV[i+1];
					uv[j+2]=meshUV[i+2]+(meshUV[i+1]-meshUV[i+2])*(1-v1/(v1-v2));
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					edges[k]=j;
					edges[k+1]=j+2;
					k+=2;
					j+=3;
				}
				else if(v0<=0&&v1<0&&v2>0){
					vertices[j]=meshVertices[i]+(meshVertices[i+2]-meshVertices[i])*(1-v2/(v2-v0));
					vertices[j+1]=meshVertices[i+1]+(meshVertices[i+2]-meshVertices[i+1])*(1-v2/(v2-v1));
					vertices[j+2]=meshVertices[i+2];
					uv[j]=meshUV[i]+(meshUV[i+2]-meshUV[i])*(1-v2/(v2-v0));
					uv[j+1]=meshUV[i+1]+(meshUV[i+2]-meshUV[i+1])*(1-v2/(v2-v1));
					uv[j+2]=meshUV[i+2];
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					edges[k]=j;
					edges[k+1]=j+1;
					k+=2;
					j+=3;
				}
				else if(v0>0&&v1<0&&v2<=0){
					vertices[j]=meshVertices[i];
					vertices[j+1]=meshVertices[i+1]+(meshVertices[i]-meshVertices[i+1])*(1-v0/(v0-v1));
					vertices[j+2]=meshVertices[i+2]+(meshVertices[i]-meshVertices[i+2])*(1-v0/(v0-v2));
					uv[j]=meshUV[i];
					uv[j+1]=meshUV[i+1]+(meshUV[i]-meshUV[i+1])*(1-v0/(v0-v1));
					uv[j+2]=meshUV[i+2]+(meshUV[i]-meshUV[i+2])*(1-v0/(v0-v2));
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					edges[k]=j+1;
					edges[k+1]=j+2;
					k+=2;
					j+=3;
				}
				else if(v0<0&&v1>0&&v2<=0){
					vertices[j]=meshVertices[i]+(meshVertices[i+1]-meshVertices[i])*(1-v1/(v1-v0));
					vertices[j+1]=meshVertices[i+1];
					vertices[j+2]=meshVertices[i+2]+(meshVertices[i+1]-meshVertices[i+2])*(1-v1/(v1-v2));
					uv[j]=meshUV[i]+(meshUV[i+1]-meshUV[i])*(1-v1/(v1-v0));
					uv[j+1]=meshUV[i+1];
					uv[j+2]=meshUV[i+2]+(meshUV[i+1]-meshUV[i+2])*(1-v1/(v1-v2));
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					edges[k]=j;
					edges[k+1]=j+2;
					k+=2;
					j+=3;
				}
				else if(v0<0&&v1<=0&&v2>0){
					vertices[j]=meshVertices[i]+(meshVertices[i+2]-meshVertices[i])*(1-v2/(v2-v0));
					vertices[j+1]=meshVertices[i+1]+(meshVertices[i+2]-meshVertices[i+1])*(1-v2/(v2-v1));
					vertices[j+2]=meshVertices[i+2];
					uv[j]=meshUV[i]+(meshUV[i+2]-meshUV[i])*(1-v2/(v2-v0));
					uv[j+1]=meshUV[i+1]+(meshUV[i+2]-meshUV[i+1])*(1-v2/(v2-v1));
					uv[j+2]=meshUV[i+2];
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					edges[k]=j;
					edges[k+1]=j+1;
					k+=2;
					j+=3;
				}
				else if(v0<0&&v1>0&&v2>0){
					vertices[j]=meshVertices[i]+(meshVertices[i+1]-meshVertices[i])*(1-v1/(v1-v0));
					vertices[j+1]=meshVertices[i+1];
					vertices[j+2]=meshVertices[i+2];
					uv[j]=meshUV[i]+(meshUV[i+1]-meshUV[i])*(1-v1/(v1-v0));
					uv[j+1]=meshUV[i+1];
					uv[j+2]=meshUV[i+2];
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					j+=3;
					vertices[j]=meshVertices[i]+(meshVertices[i+1]-meshVertices[i])*(1-v1/(v1-v0));
					vertices[j+1]=meshVertices[i+2];
					vertices[j+2]=meshVertices[i]+(meshVertices[i+2]-meshVertices[i])*(1-v2/(v2-v0));
					uv[j]=meshUV[i]+(meshUV[i+1]-meshUV[i])*(1-v1/(v1-v0));
					uv[j+1]=meshUV[i+2];
					uv[j+2]=meshUV[i]+(meshUV[i+2]-meshUV[i])*(1-v2/(v2-v0));
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					edges[k]=j;
					edges[k+1]=j+2;
					k+=2;
					j+=3;
				}
				else if(v0>0&&v1<0&&v2>0){
					vertices[j]=meshVertices[i];
					vertices[j+1]=meshVertices[i+1]+(meshVertices[i+2]-meshVertices[i+1])*(1-v2/(v2-v1));
					vertices[j+2]=meshVertices[i+2];
					uv[j]=meshUV[i];
					uv[j+1]=meshUV[i+1]+(meshUV[i+2]-meshUV[i+1])*(1-v2/(v2-v1));
					uv[j+2]=meshUV[i+2];
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					j+=3;
					vertices[j]=meshVertices[i];
					vertices[j+1]=meshVertices[i+1]+(meshVertices[i]-meshVertices[i+1])*(1-v0/(v0-v1));
					vertices[j+2]=meshVertices[i+1]+(meshVertices[i+2]-meshVertices[i+1])*(1-v2/(v2-v1));
					uv[j]=meshUV[i];
					uv[j+1]=meshUV[i+1]+(meshUV[i]-meshUV[i+1])*(1-v0/(v0-v1));
					uv[j+2]=meshUV[i+1]+(meshUV[i+2]-meshUV[i+1])*(1-v2/(v2-v1));
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					edges[k]=j+1;
					edges[k+1]=j+2;
					k+=2;
					j+=3;
				}
				else if(v0>0&&v1>0&&v2<0){
					vertices[j]=meshVertices[i];
					vertices[j+1]=meshVertices[i+1];
					vertices[j+2]=meshVertices[i+2]+(meshVertices[i+1]-meshVertices[i+2])*(1-v1/(v1-v2));
					uv[j]=meshUV[i];
					uv[j+1]=meshUV[i+1];
					uv[j+2]=meshUV[i+2]+(meshUV[i+1]-meshUV[i+2])*(1-v1/(v1-v2));
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					j+=3;
					vertices[j]=meshVertices[i];
					vertices[j+1]=meshVertices[i+2]+(meshVertices[i+1]-meshVertices[i+2])*(1-v1/(v1-v2));
					vertices[j+2]=meshVertices[i+2]+(meshVertices[i]-meshVertices[i+2])*(1-v0/(v0-v2));
					uv[j]=meshUV[i];
					uv[j+1]=meshUV[i+2]+(meshUV[i+1]-meshUV[i+2])*(1-v1/(v1-v2));
					uv[j+2]=meshUV[i+2]+(meshUV[i]-meshUV[i+2])*(1-v0/(v0-v2));
					triangles[j]=j;
					triangles[j+1]=j+1;
					triangles[j+2]=j+2;
					edges[k]=j+1;
					edges[k+1]=j+2;
					k+=2;
					j+=3;
				}
			}
		}
		if(cap&&edges.Length>2){
			Vector3 edgesAverage=Vector3.zero;
			for(int i=0;i<edges.Length;i++){
				edgesAverage+=vertices[edges[i]]/newEdges;
			}
			Vector3 x=(vertices[edges[0]]-edgesAverage).normalized;
			Vector3 y=Vector3.Cross(plane.normal,x).normalized;
			int maxIndex=0;
			float max=0;
			for(int i=0;i<edges.Length;i++){
				if(Vector3.Dot(vertices[edges[i]]-edgesAverage,vertices[edges[i]]-edgesAverage)>max){
					max=Vector3.Dot(vertices[edges[i]]-edgesAverage,vertices[edges[i]]-edgesAverage);
					maxIndex=i;
				}
			}
			max=Vector2.Distance(new Vector2(Vector3.Dot(vertices[edges[maxIndex]],x),Vector3.Dot(vertices[edges[maxIndex]],y)),Vector2.zero);
			for(int i=0,j=newTriangles;i<edges.Length;i+=2){
				if(Vector3.Dot(meshTransform.TransformDirection(Vector3.Cross(vertices[edges[i]]-edgesAverage,vertices[edges[i+1]]-edgesAverage)),plane.normal)>0){
					vertices[j]=vertices[edges[i+1]];
					vertices[j+1]=vertices[edges[i]];
					vertices[j+2]=edgesAverage;
					uv[j]=new Vector2(Vector3.Dot(vertices[edges[i+1]],x),Vector3.Dot(vertices[edges[i+1]],y))*0.5f/max+Vector2.one*0.5f;
					uv[j+1]=new Vector2(Vector3.Dot(vertices[edges[i]],x),Vector3.Dot(vertices[edges[i]],y))*0.5f/max+Vector2.one*0.5f;
					uv[j+2]=Vector2.one*0.5f;
				}
				else{
					vertices[j]=vertices[edges[i]];
					vertices[j+1]=vertices[edges[i+1]];
					vertices[j+2]=edgesAverage;
					uv[j]=new Vector2(Vector3.Dot(vertices[edges[i]],x),Vector3.Dot(vertices[edges[i]],y))*0.5f/max+Vector2.one*0.5f;
					uv[j+1]=new Vector2(Vector3.Dot(vertices[edges[i+1]],x),Vector3.Dot(vertices[edges[i+1]],y))*0.5f/max+Vector2.one*0.5f;
					uv[j+2]=Vector2.one*0.5f;
				}
				triangles[j]=j;
				triangles[j+1]=j+1;
				triangles[j+2]=j+2;
				j+=3;
			}
		}
		mesh.Clear();
		mesh.vertices=vertices;
		mesh.uv=uv;
		mesh.normals=new Vector3[triangles.Length];
		mesh.triangles=triangles;
		mesh.RecalculateNormals();
		//Debug Code Start
		print("SplitMesh total execution time: "+(Time.realtimeSinceStartup-startTime)+" seconds for "+(triangles.Length/3)+" triangles");
		//Debug Code Stop
	}
	
	public void SharpenEdges(Mesh mesh){
		//Debug Code Start
		float startTime=Time.realtimeSinceStartup;
		//Debug Code Stop
		Vector3[] meshVertices=mesh.vertices;
		Vector2[] meshUV=mesh.uv;
		int[] meshTriangles=mesh.triangles;
		Vector3[] vertices=new Vector3[meshTriangles.Length];
		Vector2[] uv=new Vector2[meshTriangles.Length];
		int[] triangles=new int[meshTriangles.Length];
		for(int i=0;i<meshTriangles.Length;i++){
			vertices[i]=meshVertices[meshTriangles[i]];
			if(meshUV.Length>0){
				uv[i]=meshUV[meshTriangles[i]];
			}
			triangles[i]=i;
		}
		mesh.Clear();
		mesh.vertices=vertices;
		mesh.uv=uv;
		mesh.normals=new Vector3[triangles.Length];
		mesh.triangles=triangles;
		mesh.RecalculateNormals();
		//Debug Code Start
		print("SharpenEdges total execution time: "+(Time.realtimeSinceStartup-startTime)+" seconds for "+(triangles.Length/3)+" triangles");
		//Debug Code Stop
	}
}
