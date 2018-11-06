# find . -name 'project.pbxproj' -type f -exec plutil -convert xml1 -o {} {} \;
plutil -convert xml1 -o project.pbxproj project.pbxproj
