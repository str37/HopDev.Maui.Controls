// Mac Catalyst implementations — Phase 4
// 
// macOS title bar support will map to:
// - NSWindow.titlebarAppearsTransparent
// - NSFullSizeContentViewWindowMask  
// - Traffic light (close/minimize/zoom) inset measurement
//
// Scale service will use NSScreen.backingScaleFactor (which doesn't lie on Mac).
