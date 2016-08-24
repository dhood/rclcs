﻿using System;
using System.Runtime.InteropServices;
using System.Reflection;
namespace ROS2Sharp
{
	public class Subscription<T>:Executable
		where T : MessageBase, new()
	{
		private rosidl_message_type_support_t TypeSupport;
		private rcl_subscription InternalSubscription;
		public Node RosNode{ get; private set;}
		public string TopicName { get; private set; }
		public rcl_subscription_options_t SubscriptionOptions{ get; private set; }
		public event EventHandler<MessageRecievedEventArgs<T>> MessageRecieved;
		public Subscription (Node _node, string _topicName)
		{
			RosNode = _node;
			TopicName = _topicName;

			//TypeSupport = IntPtr.Zero;
			Type messsageType = typeof(T);
			foreach (var item in messsageType.GetMethods()) {

				if (item.IsStatic ) {
					if (item.Name.Contains ("rosidl_typesupport_introspection_c_get_message")) {
						TypeSupport = (rosidl_message_type_support_t)Marshal.PtrToStructure((IntPtr)item.Invoke (null, null),typeof(rosidl_message_type_support_t));
					}
				}
			}
			SubscriptionOptions = rcl_subscription.get_default_options ();
			InternalSubscription = new rcl_subscription (RosNode, TypeSupport, TopicName,SubscriptionOptions);
		}
		public rcl_subscription_t NativeSubscription
		{
			get{return InternalSubscription.NativeSubscription;}
		}
		public rcl_subscription NativeWrapper
		{
			get{ return InternalSubscription;}
		}

		public override void Execute()
		{
			bool success = false;
			T message = InternalSubscription.TakeMessage<T> (out success);
			if (success) {
				if (MessageRecieved != null)
					MessageRecieved (this,new MessageRecievedEventArgs<T> (message));
			}
		}

	}
	public class rcl_subscription
	{
		private rcl_subscription_t subscription;
		private rcl_node_t native_node;
		public rcl_subscription(Node _node, rosidl_message_type_support_t _type_support, string _topic_name, rcl_subscription_options_t _options)
		{
			subscription = rcl_get_zero_initialized_subscription ();
			native_node = _node.NativeNode;
			rcl_subscription_init (ref subscription,ref native_node,ref _type_support, _topic_name,ref _options);
		}
		~rcl_subscription()
		{
			rcl_subscription_fini (ref subscription, ref native_node);
		}
		public rcl_subscription_t NativeSubscription
		{
			get{ return subscription;}
		}

		public static rcl_subscription_options_t get_default_options()
		{
			return rcl_subscription_get_default_options ();
		}
		public T TakeMessage<T>(out bool success)
			where T: MessageBase, new()
		{
			T msg = new T ();
			rmw_message_info_t message_info = new rmw_message_info_t();
			//IntPtr msg_ptr = Marshal.AllocHGlobal (Marshal.SizeOf (msg));
			//IntPtr message_info_ptr = Marshal.AllocHGlobal(Marshal.SizeOf(message_info));

			int ret = rcl_take (ref subscription, msg, message_info);
			RCLReturnValues ret_val = (RCLReturnValues)ret;
			//Console.WriteLine (ret_val);
			/*return RCL_RET_OK if the message was published, or
			*         RCL_RET_INVALID_ARGUMENT if any arguments are invalid, or
			*         RCL_RET_SUBSCRIPTION_INVALID if the subscription is invalid, or
			*         RCL_RET_BAD_ALLOC if allocating memory failed, or
			*         RCL_RET_SUBSCRIPTION_TAKE_FAILED if take failed but no error
				*         occurred in the middleware, or
			*         RCL_RET_ERROR if an unspecified error occurs.*/
			bool take_message_success = false;
			switch (ret_val) {
			case RCLReturnValues.RCL_RET_INVALID_ARGUMENT:
				throw new RCLInvalidArgumentException ();
				break;
			case RCLReturnValues.RCL_RET_SUBSCRIPTION_INVALID:
				throw new RCLSubscriptionInvalidException ();
				break;
			case RCLReturnValues.RCL_RET_BAD_ALLOC:
				throw new RCLBadAllocException ();
				break;
			case RCLReturnValues.RCL_RET_SUBSCRIPTION_TAKE_FAILED:
				//throw new RCLSubscriptonTakeFailedException ();
				take_message_success = false;
				//Marshal.FreeHGlobal (msg_ptr);
				break;
			case RCLReturnValues.RCL_RET_ERROR:
				throw new RCLErrorException ();
				break;
			case RCLReturnValues.RCL_RET_OK:
				{
					take_message_success = true;
					//Is this needed -> How to two way marshal structures that were passed as void ptrs?
					//msg = Marshal.PtrToStructure<T> (msg_ptr);

				}
				break;
			default:
				break;
			}
			success = take_message_success;
			//Marshal.FreeHGlobal (message_info_ptr);
		
			return msg;
		}

		[DllImport("librcl.so")]
		extern static rcl_subscription_t rcl_get_zero_initialized_subscription();

		[DllImport("librcl.so")]
		extern static int rcl_subscription_init(ref rcl_subscription_t subscription, ref rcl_node_t node, ref rosidl_message_type_support_t typesupport, string topic_name, ref rcl_subscription_options_t options);

		[DllImport("librcl.so")]
		extern static int rcl_subscription_fini (ref rcl_subscription_t subscription, ref rcl_node_t node);

		[DllImport("librcl.so")]
	    extern static rcl_subscription_options_t rcl_subscription_get_default_options ();

		[DllImport("librcl.so")]
		extern static int rcl_take(ref rcl_subscription_t subscription, [In,Out] MessageBase ros_message,[In,Out] rmw_message_info_t message_info);

		[DllImport("librcl.so")]
		extern static string rcl_subscription_get_topic_name(ref rcl_subscription_t subscription);

		[DllImport("librcl.so")]
		extern static IntPtr rcl_subscription_get_options(ref rcl_subscription_t subscription);
			
	}
	public struct rcl_subscription_t
	{
		IntPtr impl;
	}
	public struct rcl_subscription_options_t
	{
		rmw_qos_profile_t qos;
		bool ignore_local_publications;
		rcl_allocator_t allocator;
	}
}
