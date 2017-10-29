using System.Linq;
using System.Collections.Generic;
using GrandTheftMultiplayer.Server.API;
using GrandTheftMultiplayer.Server.Elements;
using GrandTheftMultiplayer.Server.Managers;
using GrandTheftMultiplayer.Shared.Math;

namespace ConsistentDelete
{
    #region ObjectRemover
    public class ObjectRemover
    {
        public int Model;
        public Vector3 Location;
        public float Radius;
        private ColShape DelColShape;

        public ObjectRemover(int model, Vector3 location, float radius = 1.0f)
        {
            Model = model;
            Location = location;
            Radius = radius;
        }

        public void CreateColShape()
        {
            DelColShape = API.shared.createSphereColShape(Location, ConsistentDelete.ColShapeRadius);
            DelColShape.onEntityEnterColShape += (shape, entityHandle) =>
            {
                Client player = API.shared.getPlayerFromHandle(entityHandle);
                if (player == null) return;

                // why delayed? reasons
                API.shared.delay(200, true, () => API.shared.deleteObject(player, Location, Model, Radius));
            };
        }

        public void DeleteColShape()
        {
            if (DelColShape != null) API.shared.deleteColShape(DelColShape);
        }
    }
    #endregion

    public class ConsistentDelete : Script
    {
        public static float ColShapeRadius = 25.0f; // change this from meta.xml

        int RemoverID = 0;
        Dictionary<int, ObjectRemover> ObjectsToRemove = new Dictionary<int, ObjectRemover>();

        #region Exported Methods
        public int registerDeletion(int model, Vector3 location, float radius = 1f)
        {
            RemoverID++;

            ObjectRemover new_remover = new ObjectRemover(model, location, radius);
            new_remover.CreateColShape();
            ObjectsToRemove.Add(RemoverID, new_remover);

            // delete objects for players in colshape radius, why? reasons
            foreach (Client player in API.getAllPlayers())
            {
                if (player.position.DistanceTo2D(location) <= ColShapeRadius) API.deleteObject(player, location, model, radius);
            }

            return RemoverID;
        }

        public void removeDeletion(int ID)
        {
            if (!ObjectsToRemove.ContainsKey(ID)) return;
            ObjectsToRemove[ID].DeleteColShape();
            ObjectsToRemove.Remove(ID);
        }

        public int[] getDeletionIDs()
        {
            return ObjectsToRemove.Keys.ToArray();
        }
        #endregion

        public ConsistentDelete()
        {
            API.onResourceStart += ConsistentDelete_Init;
            API.onResourceStop += ConsistentDelete_Exit;
        }

        #region Events
        public void ConsistentDelete_Init()
        {
            // load radius from meta because it was requested by a good friend
            if (API.hasSetting("colShapeRadius")) ColShapeRadius = API.getSetting<float>("colShapeRadius");
        }

        public void ConsistentDelete_Exit()
        {
            foreach (ObjectRemover item in ObjectsToRemove.Values) item.DeleteColShape();
            ObjectsToRemove.Clear();
        }
        #endregion
    }
}
