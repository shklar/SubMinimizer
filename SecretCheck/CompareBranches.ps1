# this script must be run manually at any folder containing repository for check
# change lines for appropriate folder getting and branch for check
$branch_name = 'mov_secr_az'
cd ('C:\projects\Sub_prd')

CompareBranches

function CompareBranches()
{
   Git diff mov_secr_az  mov_secr_az_mrg
}
