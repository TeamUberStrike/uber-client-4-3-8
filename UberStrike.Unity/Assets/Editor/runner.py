#!/usr/bin/python

from UpdateXcode import *
import os, uuid, sys, types, re
import StringIO
import plistlib
import hashlib
import logging
import syslog



if __name__ == "__main__":
	syslog.openlog( 'Prime31' )
	shouldProcessFolders = len( sys.argv ) == 2

	if not shouldProcessFolders:
		# grab our arguments and only process this single plugin
		projectPath = sys.argv[1]
		unityProjectPath = sys.argv[2]
		pluginName = sys.argv[3]

		PluginHelper.processPlugin( projectPath, unityProjectPath, pluginName )
		exit

	syslog.syslog( syslog.LOG_ALERT, 'Processing all folders' )

	# we need to iterate all the directories to get our folders
	projectPath = sys.argv[1]
	unityProjectPath = os.getcwd().replace( '/Assets/Editor', '' )

	# fetch all the plugin folders
	pluginFolders = PluginHelper.getAllPluginFoldersThatNeedProcessing( os.path.join( unityProjectPath, "Assets/Editor" ) )

	# process each one
	for plugin in pluginFolders:
		PluginHelper.processPlugin( projectPath, unityProjectPath, plugin )


