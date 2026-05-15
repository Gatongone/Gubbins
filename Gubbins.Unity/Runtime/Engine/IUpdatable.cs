// Copyright ©2024 Gatongone
// Author: Gatongone
// Email: gatongone@gmail.com
// Created On: 2024/03/17-21:04:28
// Github: https://github.com/Gatongone

namespace Gubbins.Unity
{
    public interface IInitUpdatable
    {
        void OnInitUpdate();
    }

    public interface IEarlyUpdatable
    {
        void OnEarlyUpdate();
    }

    public interface IPostLateUpdatable
    {
        void OnPostLateUpdate();
    }

    public interface IPreLateUpdatable
    {
        void OnPreLateUpdate();
    }

    public interface IPreUpdatable
    {
        void OnPreUpdate();
    }

    public interface IFixedUpdatable
    {
        void OnFixedUpdate();
    }

    public interface IUpdatable
    {
        void OnUpdate();
    }
}