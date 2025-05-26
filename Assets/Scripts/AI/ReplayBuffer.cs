using System;
using System.Collections.Generic;
using System.Linq;

public struct Transition
{
    public string RaceName;
    public int State;
    public int Action;
    public float Reward;
    public int NextState;
    public float Priority;
}

public class PrioritizedReplayBuffer
{
    public int capacity;
    public List<Transition> buffer;
    public List<float> priorities;
    public System.Random rnd = new System.Random();

    public PrioritizedReplayBuffer(int capacity = 10000)
    {
        this.capacity = capacity;
        buffer = new List<Transition>(capacity);
        priorities = new List<float>(capacity);
    }

    public void Add(Transition t)
    {
        // jeśli pełny, usuń najstarszy
        if (buffer.Count >= capacity)
        {
            buffer.RemoveAt(0);
            priorities.RemoveAt(0);
        }
        buffer.Add(t);
        priorities.Add(t.Priority);
    }

    public Transition[] Sample(int batchSize)
    {
        // suma priorytetów
        float sum = priorities.Sum();
        var sample = new Transition[Math.Min(batchSize, buffer.Count)];
        for (int i = 0; i < sample.Length; i++)
        {
            float pick = (float)(rnd.NextDouble() * sum);
            float acc = 0;
            for (int j = 0; j < buffer.Count; j++)
            {
                acc += priorities[j];
                if (acc >= pick)
                {
                    sample[i] = buffer[j];
                    break;
                }
            }
        }
        return sample;
    }

    public int Count => buffer.Count;
}