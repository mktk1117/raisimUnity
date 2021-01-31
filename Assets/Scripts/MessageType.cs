/*
 * MIT License
 * 
 * Copyright (c) 2019, Dongho Kang, Robotics Systems Lab, ETH Zurich
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;

namespace raisimUnity
{
    // socket commands from client
    public enum ServerMessageType : Int32
    {
        Initialization = 0,
        ObjectPositionUpdate,
        Status,
        NoMessage,
        ContactInfoUpdate,
        ConfigXml,
        VisualInitialization,
        VisualPositionUpdate,
        Reset,
    }

    enum ClientMessageType : Int32
    {    
        RequestObjectPosition = 0,
        RequestInitializeObjects,
        RequestResource,                 // request mesh, texture. etc files
        RequestChangeRealtimeFactor,
        RequestContactSolverDetails,
        RequestPause,
        RequestResume,
        RequestContactInfos,
        RequestConfigXML,
        RequestInitializeVisuals,
        RequestVisualPosition,
        RequestServerStatus,
    }

    public enum ServerStatus : Int32
    {
        StatusRendering = 0,
        StatusHibernating,
        StatusTerminating,
    }
}