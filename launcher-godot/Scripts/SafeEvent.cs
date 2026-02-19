using System;
using System.Collections.Generic;
using Godot;

namespace LauncherGodot.Scripts;

public class SafeEvent<T> {
    private readonly List<Action<T>> _handlers = [];
    
    public void Subscribe(Node self, Action<T> handler) {
        _handlers.Add(handler);
        self.TreeExiting += () => _handlers.Remove(handler);
    }
    
    public void Invoke(T arg) {
        foreach (Action<T> handler in _handlers) {
            handler(arg);
        }
    }
}
