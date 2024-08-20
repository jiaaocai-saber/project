using UnityEngine;

public class EntityCreator : MonoBehaviour
{
    public GameObject monsterPrefab;  // 用于生成monster的预制体
    public GameObject thingPrefab;    // 用于生成things的预制体

    public void CreateEntity(string entityType, string location)
    {
        GameObject prefabToInstantiate = null;

        // 根据 entityType 选择预制体
        if (entityType == "monster")
        {
            prefabToInstantiate = monsterPrefab;
        }
        else if (entityType == "thing")
        {
            prefabToInstantiate = thingPrefab;
        }

        if (prefabToInstantiate != null)
        {
            GameObject newEntity = Instantiate(prefabToInstantiate);
            newEntity.name = entityType;

            // 根据 location 设置位置，这里简单示例，可以根据需要扩展
            if (location == "in the room")
            {
                newEntity.transform.position = new Vector3(6, 1, 9);  // 设置在房间内的位置
            }
            else if (location == "behind the wall")
            {
                newEntity.transform.position = new Vector3(17, 1, 12);  // 设置在墙后的位置
            }

            Debug.Log($"Entity '{entityType}' created at '{location}'.");
        }
        else
        {
            Debug.LogError("No prefab assigned for the entity type.");
        }
    }
}
