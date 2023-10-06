namespace PieceManager;

public class SnapPoint
{
    public SnapPoint(GameObject gameObject) { GameObject = gameObject; }

    public GameObject GameObject { get; }
    public bool IsInUse { get; set; }
}

[PublicAPI]
public class SnapPointMaker
{
    public enum SnapPointType
    {
        Vertices,
        Center,
        Poles,
        Equator
    }

    private static readonly List<GameObject> ObjectsToApplySnaps = new();
    private static readonly List<SnapPoint> SnapPoints = new();

    public static void AddObjectForSnapPoints(GameObject obj) { ObjectsToApplySnaps.Add(obj); }

    public static void ApplySnapPoints(SnapPointType snapPointType)
    {
        foreach (var gameObject in ObjectsToApplySnaps) GrabVerticesAssignSnaps(gameObject, snapPointType);
    }


    private static void GrabVerticesAssignSnaps(GameObject obj, SnapPointType snapPointType)
    {
        var collider = obj.GetComponent<Collider>();

        if (collider is BoxCollider boxCollider)
        {
            var vertices = GetBoxColliderSnapPoints(obj, boxCollider, snapPointType);
            AttachSnapPoints(obj, vertices);
        } else if (collider is SphereCollider sphereCollider)
        {
            var vertices = GetSphereColliderSnapPoints(obj, sphereCollider, snapPointType);
            AttachSnapPoints(obj, vertices);
        } else
        {
            DebugWarning($"Unsupported collider type for {obj.name}: " + collider.GetType());
        }
    }

    private static Vector3[] GetBoxColliderSnapPoints(GameObject obj, BoxCollider col, SnapPointType snapPointType)
    {
        var vertices = new List<Vector3>();
        var trans = obj.transform;

        var min = col.center - col.size * 0.5f;
        var max = col.center + col.size * 0.5f;

        if (snapPointType == SnapPointType.Vertices || snapPointType == SnapPointType.Center)
        {
            // Add vertices
            vertices.Add(trans.TransformPoint(new Vector3(min.x, min.y, min.z)));
            vertices.Add(trans.TransformPoint(new Vector3(min.x, min.y, max.z)));
            vertices.Add(trans.TransformPoint(new Vector3(min.x, max.y, min.z)));
            vertices.Add(trans.TransformPoint(new Vector3(min.x, max.y, max.z)));
            vertices.Add(trans.TransformPoint(new Vector3(max.x, min.y, min.z)));
            vertices.Add(trans.TransformPoint(new Vector3(max.x, min.y, max.z)));
            vertices.Add(trans.TransformPoint(new Vector3(max.x, max.y, min.z)));
            vertices.Add(trans.TransformPoint(new Vector3(max.x, max.y, max.z)));
        }

        if (snapPointType == SnapPointType.Center)
            // Add center
            vertices.Add(trans.TransformPoint(col.center));

        return vertices.ToArray();
    }

    private static Vector3[] GetSphereColliderSnapPoints(GameObject obj, SphereCollider col,
        SnapPointType snapPointType)
    {
        var vertices = new List<Vector3>();
        var trans = obj.transform;

        if (snapPointType == SnapPointType.Poles || snapPointType == SnapPointType.Center)
        {
            // Add points at top and bottom
            vertices.Add(trans.TransformPoint(new Vector3(0, col.radius, 0)));
            vertices.Add(trans.TransformPoint(new Vector3(0, -col.radius, 0)));
        }

        if (snapPointType == SnapPointType.Equator)
        {
            // Add cardinal directions on the equator
            vertices.Add(trans.TransformPoint(new Vector3(-col.radius, 0, 0)));
            vertices.Add(trans.TransformPoint(new Vector3(col.radius, 0, 0)));
            vertices.Add(trans.TransformPoint(new Vector3(0, 0, -col.radius)));
            vertices.Add(trans.TransformPoint(new Vector3(0, 0, col.radius)));
        }

        if (snapPointType == SnapPointType.Center)
            // Add center
            vertices.Add(trans.TransformPoint(col.center));

        return vertices.ToArray();
    }

    private static Vector3[] GetColliderVertexPosRotated(GameObject obj)
    {
        var vertices = new Vector3[8];
        var col = obj.GetComponentInChildren<BoxCollider>();
        if (col == null) return vertices;
        var trans = obj.transform;
        var min = col.center - col.size * 0.5f;
        var max = col.center + col.size * 0.5f;
        vertices[0] = trans.TransformPoint(new Vector3(min.x, min.y, min.z));
        vertices[1] = trans.TransformPoint(new Vector3(min.x, min.y, max.z));
        vertices[2] = trans.TransformPoint(new Vector3(min.x, max.y, min.z));
        vertices[3] = trans.TransformPoint(new Vector3(min.x, max.y, max.z));
        vertices[4] = trans.TransformPoint(new Vector3(max.x, min.y, min.z));
        vertices[5] = trans.TransformPoint(new Vector3(max.x, min.y, max.z));
        vertices[6] = trans.TransformPoint(new Vector3(max.x, max.y, min.z));
        vertices[7] = trans.TransformPoint(new Vector3(max.x, max.y, max.z));

        return vertices;
    }

    private static void AttachSnapPoints(GameObject objecttosnap, Vector3[] vector3S)
    {
        foreach (var point in vector3S)
        {
            var snapPoint = AddSnapPoint(objecttosnap, point);
            snapPoint.GameObject.SetActive(false);
        }
    }

    public static SnapPoint AddSnapPoint(GameObject parent, Vector3 position)
    {
        foreach (var snapPoint in SnapPoints)
            if (!snapPoint.IsInUse)
            {
                snapPoint.GameObject.transform.SetParent(parent.transform, false);
                snapPoint.GameObject.transform.position = position;
                snapPoint.IsInUse = true;
                return snapPoint;
            }

        var snapPointObject = new GameObject("_snappoint");
        snapPointObject.tag = "snappoint";
        snapPointObject.layer = 10;
        snapPointObject.transform.SetParent(parent.transform, false);
        snapPointObject.transform.position = position;

        var newSnapPoint = new SnapPoint(snapPointObject);
        newSnapPoint.IsInUse = true;
        SnapPoints.Add(newSnapPoint);

        return newSnapPoint;
    }

    public static void ReleaseSnapPoint(SnapPoint snapPoint)
    {
        snapPoint.GameObject.transform.parent = null;
        snapPoint.IsInUse = false;
    }
}