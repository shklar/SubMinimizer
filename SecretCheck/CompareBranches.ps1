﻿# this script must be run manually at any folder containing repository for check
# change lines for appropriate folder getting and branch for check
cd ('C:\projects\Sub_prd')

CompareBranches

# function used for comparison of two branches current state
# now some branches have in history sensitive information so committing changes in them into master is forbidden
# we create branch with current master state, copy there dirty branch last file trees state
# now we have in new branch needed changes, last master state and no sensitive info
#  change branch names to dirty branch name and new branch name.
# run script and make sure no significant difference with dirty branch last state exist
# now we can delete dirty branch and commit from copy without sensitive info but with needed changes into master
function CompareBranches()
{
   Git diff mov_secr_az  mov_secr_az_mrg
}
