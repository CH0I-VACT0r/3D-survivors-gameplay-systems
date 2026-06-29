using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI; // NavMeshAgent 접근을 위해 추가

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;
    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    void Awake() { Instance = this; }

    public void Preload(GameObject prefab, int count)
    {
        if (!poolDictionary.ContainsKey(prefab))
            poolDictionary.Add(prefab, new Queue<GameObject>());

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab, this.transform);
            obj.SetActive(false);

            if (!obj.TryGetComponent<PoolMember>(out var pm))
                pm = obj.AddComponent<PoolMember>();

            pm.myPrefab = prefab;
            poolDictionary[prefab].Enqueue(obj);
        }
    }

    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) { return null; }

        if (!poolDictionary.ContainsKey(prefab))
            poolDictionary.Add(prefab, new Queue<GameObject>());

        GameObject obj = null;

        // 1. 풀에서 꺼내는 경우 (재사용)
        if (poolDictionary[prefab].Count > 0)
        {
            obj = poolDictionary[prefab].Dequeue();

            // [해결책 2] 물리/NavMesh가 있는 경우 안전하게 이동
            // (SetActive(true) 전에 위치를 확실히 잡아야 함)

            // NavMeshAgent가 있다면 Warp 사용 (transform.position 무시됨 방지)
            NavMeshAgent agent = obj.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.Warp(position);
                agent.transform.rotation = rotation;
            }
            // CharacterController가 있다면 껐다 켜서 이동 (물리 충돌 방지)
            else if (obj.TryGetComponent<CharacterController>(out CharacterController cc))
            {
                cc.enabled = false;
                obj.transform.SetPositionAndRotation(position, rotation);
                cc.enabled = true;
            }
            // 일반적인 경우
            else
            {
                obj.transform.SetPositionAndRotation(position, rotation);
            }
        }
        // 2. 새로 생성해야 하는 경우 (신규)
        else
        {
            // [해결책 1] 생성과 동시에 위치/회전 지정
            // (0,0,0)이나 프리팹 좌표를 거치지 않고 바로 목표 지점에서 Awake/OnEnable 실행됨
            obj = Instantiate(prefab, position, rotation, this.transform);

            if (!obj.TryGetComponent<PoolMember>(out var pm))
                pm = obj.AddComponent<PoolMember>();
            pm.myPrefab = prefab;
        }

        // 3. 최종 활성화 (이때 OnEnable 실행)
        // 신규 생성의 경우 이미 켜져있을 수 있지만, 중복 호출되어도 안전함
        obj.SetActive(true);

        return obj;
    }

    public void ReturnToPool(GameObject obj)
    {
        PoolMember member = obj.GetComponent<PoolMember>();
        if (member == null)
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        poolDictionary[member.myPrefab].Enqueue(obj);
    }
}

// (PoolMember 클래스는 그대로 유지)
public class PoolMember : MonoBehaviour
{
    public GameObject myPrefab;
}
