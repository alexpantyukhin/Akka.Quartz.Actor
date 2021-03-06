﻿using System;
using Akka.Actor;
using Akka.Quartz.Actor.Commands;
using Akka.Quartz.Actor.Events;
using Quartz.Impl;
using IScheduler = Quartz.IScheduler;

namespace Akka.Quartz.Actor
{
    /// <summary>
    /// The persistent quartz scheduling actor. Handles a single quartz scheduler
    /// and processes CreatePersistentJob and RemoveJob messages.
    /// </summary>
    public class QuartzPersistentActor : QuartzActor
    {
        private readonly IScheduler _scheduler;

        public QuartzPersistentActor()
        {
            _scheduler = new StdSchedulerFactory().GetScheduler();
            if (!_scheduler.Context.ContainsKey(QuartzPersistentJob.SysKey))
            {
                _scheduler.Context.Add(QuartzPersistentJob.SysKey, Context.System);
            }
            else
            {
                _scheduler.Context.Remove(QuartzPersistentJob.SysKey);
                _scheduler.Context.Add(QuartzPersistentJob.SysKey, Context.System);
            }
        }

        protected override bool Receive(object message)
        {
            return message.Match().With<CreatePersistentJob>(CreateJobCommand).With<RemoveJob>(RemoveJobCommand).WasHandled;
        }

        private void CreateJobCommand(CreatePersistentJob createJob)
        {
            if (createJob.To == null)
            {
                Context.Sender.Tell(new CreateJobFail(null, null, new ArgumentNullException("createJob.To")));
            }
            if (createJob.Trigger == null)
            {
                Context.Sender.Tell(new CreateJobFail(null, null, new ArgumentNullException("createJob.Trigger")));
            }
            else
            {

                try
                {
                    var job =
                    QuartzPersistentJob.CreateBuilderWithData(createJob.To, createJob.Message)
                        .WithIdentity(createJob.Trigger.JobKey)
                        .Build();
                    _scheduler.ScheduleJob(job, createJob.Trigger);

                    Context.Sender.Tell(new JobCreated(createJob.Trigger.JobKey, createJob.Trigger.Key));
                }
                catch (Exception ex)
                {
                    Context.Sender.Tell(new CreateJobFail(createJob.Trigger.JobKey, createJob.Trigger.Key, ex));
                }
            }
        }
    }
}
