A small mesh library implemented a few years ago. Features include mesh projection (decals), mesh puncturing, mesh splitting, and mesh sharpening. Written in Unity. 

void ProjectionMesh()

	Inputs
		
		meshTransform: The transform to which the mesh is attached. Used to transform the mesh vertices into world space.
		
		mesh: The mesh that we want to add a decal to. The mesh IS NOT modified by the function.
		
		ray: The ray that is to place the decal. The first intersection is used.
		
		radius: The max size of the decal. All geometry beyond is discarded.
		
		material: The material that is to be applied to the decal.
		
	Outputs
		
		The function creates a decal and parents it to the transform provided. Can be used for sprays/splatters.
		The decal geometry is taken from the mesh provided (although it is modified to be as small as possible).
		The decal material is mapped procedurally, so orientation varies (i.e. the decal doesn't always have the same rotation).

void PunctureMesh()
	
	Inputs
	
		meshTransform: The transform to which the mesh is attached. Used to transform the mesh vertices into world space.
		
		mesh: The mesh that we want to add a puncture to. The mesh IS modified by the function.
		
		ray: The ray that is to place the puncture. All intersections are used.
		
		radius: The radius of the puncture.
		
		planes: The quality of the puncture. 3 planes make the puncture appear as a triangle, 4 as a square, et cetera.
		
	Outputs
		
		The function punctures the mesh provided. Can be used for holes or procedural destruction.

void SplitMesh()
	
	Inputs
		
		meshTransform: The transform to which the mesh is attached. Used to transform the mesh vertices into world space.
		
		mesh: The mesh that we want to split. The mesh IS modified by the function.
		
		plane: The plane that is used to split the mesh.
		
		cap: Toggle to place a "cap" over the split.
		
	Outputs
		
		The function splits the provided mesh. Only the top half (as defined by the plane provided) is returned.
		In order to split a mesh into two, first copy the object, and then call the function twice.
		Once with the place normal facing up, and once with it facing down.

void SharpenEdges()
	
	Inputs
	
		mesh -> The mesh we want to sharpen. The mesh IS modified by the function.
		
	Outputs
		
		The function removes smoothed normals from the mesh. Can be used for low poly effects.
