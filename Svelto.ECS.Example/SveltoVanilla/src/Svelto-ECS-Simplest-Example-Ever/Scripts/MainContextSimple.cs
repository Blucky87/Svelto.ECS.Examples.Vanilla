﻿using System;
using System.Collections;
using Svelto.ECS.Example.Simple.SimpleClass.Engine;
using Svelto.ECS.Example.Simple.SimpleClass.Entity;
using Svelto.ECS.Example.Simple.SimpleStruct.Engine;
using Svelto.ECS.Example.Simple.SimpleStruct.Entity;
using Svelto.ECS.Schedulers;
using Svelto.WeakEvents;

namespace Svelto.ECS.Example.Simple
{
    #region comment
    /// <summary>
    /// with Unity there is no real reason to use any different than the 
    ///provided UnitySubmissionEntityViewScheduler. However Svelto.ECS
    ///has been written to be platform indipendent, so that you can
    ///write your own scheduler on another platform.
    ///The following scheduler has been made just for the sole purpose
    ///to show the simplest execution possible, which is add entityViews
    ///in the same moment they are built.
    /// </summary>
    #endregion
    class SimpleSubmissionEntityViewScheduler : EntitySubmissionScheduler
    {
        public override void Schedule(WeakAction submitEntityViews)
        {
            Scheduler(submitEntityViews).Run();
        }

        IEnumerator Scheduler(WeakAction submitEntityViews)
        {
            while (true)
            {
                if (submitEntityViews.IsValid)
                    submitEntityViews.Invoke();
                else
                    yield break;

                System.Threading.Thread.Sleep(1);

                yield return null;
            }
        }
    }

    #region comment
    /// <summary>
    ///The Context is the framework starting point.
    ///As Composition root, it gives back to the coder the responsibility to create, 
    ///initialize and inject dependencies.
    ///Every application can have more than one context and every context can have one
    ///or more composition roots.
    ///Svelto.Context is actually separated by Svelto.ECS has they can live indipendently
    /// </summary>
    #endregion
    public class SimpleCompositionRoot
    {
        public SimpleCompositionRoot()
        {
            Run().Run();

            Console.ReadKey();
        }

        private IEnumerator Run()
        {
            #region comment  
            //An EngineRoot holds all the engines created so far and is 
            //responsible of the injection of the entity entityViews inside every
            //relative engine.
            #endregion
            _enginesRoot = new EnginesRoot(new SimpleSubmissionEntityViewScheduler());

            //an EnginesRoot must never
            IEntityFactory entityFactory = _enginesRoot.GenerateEntityFactory();
            IEntityFunctions entityFunctions = _enginesRoot.GenerateEntityFunctions();

            _enginesRoot.AddEngine(new SimpleEngine(entityFunctions));
            _enginesRoot.AddEngine(new SimpleStructEngine());
            
            var watch = new System.Diagnostics.Stopwatch();
            
            //number of nodes/implementor not 1:1 to component
            object[] implementors = new object[1];
            int i = 0;
#if PROFILE
            entityFactory.Preallocate<SimpleEntityDescriptor>(10000000);

            watch.Start();

            for (i = 0; i < 10000000; i++)
            {
#endif
                implementors[0] = new SimpleImplementor("simpleEntity");
                    
                entityFactory.BuildEntity<SimpleEntityDescriptor>(i, implementors);
#if PROFILE
            }


            watch.Stop();

            Utility.Console.Log(watch.ElapsedMilliseconds.ToString());

            entityFactory.Preallocate<SimpleStructEntityDescriptor>(10000000);


            watch.Restart();

            for (i = 0; i < 10000000; i++)
#endif
                entityFactory.BuildEntityInGroup<SimpleStructEntityDescriptor>(i, 0);
#if PROFILE
            watch.Stop();

            Utility.Console.Log(watch.ElapsedMilliseconds.ToString());

            System.Environment.Exit(0);
#endif

            Utility.Console.Log("built");

            yield break;
        }

        EnginesRoot _enginesRoot;
    }

    namespace SimpleClass
    {
        namespace Entity
        {
            public interface ISimpleComponent
            {
                string name { get; }
            }

            class SimpleImplementor : ISimpleComponent
            {
                public SimpleImplementor(string name)
                {
                    this.name = name;
                }

                public string name { get; }
            }

            class SimpleEntityDescriptor : GenericEntityDescriptor<SimpleEntityView>
            {}
        }

        namespace Engine
        {
            public class SimpleEngine : SingleEntityViewEngine<SimpleEntityView>
            {
                IEntityFunctions _entityFunctions;

                public SimpleEngine(IEntityFunctions entityFunctions)
                {
                    _entityFunctions = entityFunctions;
                }

                protected override void Add(SimpleEntityView entityView)
                {
                    Utility.Console.Log("EntityView Added");

                    _entityFunctions.RemoveEntity<SimpleEntityDescriptor>(entityView.ID);
                }

                protected override void Remove(SimpleEntityView entityView)
                {
                    Utility.Console.Log(entityView.simpleComponent.name + "EntityView Removed");
                }
            }
        }

        public class SimpleEntityView : EntityView
        {
            public ISimpleComponent simpleComponent;
        }
    }

    namespace SimpleStruct
    {
        namespace Entity
        {
            class SimpleStructEntityDescriptor : MixedEntityDescriptor<EntityStructBuilder<SimpleEntityStruct>>
            { }
        }
    }

    namespace SimpleStruct
    {
        namespace Engine
        { 
            struct SimpleEntityStruct : IEntityStruct
            {
                int _id;

                int IEntityView.ID { get { return _id; } }
                int IEntityStruct.ID { set { _id = value; } }

                public int counter;
            }

            public class SimpleStructEngine : IQueryingEntityViewEngine
            {
                public IEngineEntityViewDB entityViewsDB { get; set; }

                public void Ready()
                {
                    Update().Run();
                }

                IEnumerator Update()
                {
                    Utility.Console.Log("Task Waiting");

                    while (true)
                    {
                        var entityViews = entityViewsDB.QueryGroupedEntityViewsAsArray<SimpleEntityStruct>(0, out int count);

                        if (count > 0)
                        {
                            for (int i = 0; i < count; i++)
                                AddOne(ref entityViews[i].counter);

                            Utility.Console.Log("Task Done");

                            yield break;
                        }

                        yield return null;
                    }
                }

                static void AddOne(ref int counter)
                {
                    counter += 1;
                }
            }
        }
    }

    //////

    //region      
    //a Unity context is a platform specific context wrapper. 
    //Unity will drive the ICompositionRoot interface.
    //OnContextCreated is called during the Awake of this MB
    //OnContextInitialized is called one frame after the MB started
    //OnContextDestroyed is called when the MB is destroyed
    //endregion
    public class Program
    {
        static void Main(string[] args)
        {
            compositionRoot = new SimpleCompositionRoot();
        }

        private static SimpleCompositionRoot compositionRoot;
    }
}

