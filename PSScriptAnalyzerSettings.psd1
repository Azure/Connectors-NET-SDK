@{
    Severity     = @('Warning', 'Error')
    ExcludeRules = @(
        # Setup scripts use Write-Host intentionally for colored interactive output.
        'PSAvoidUsingWriteHost'
    )
}
