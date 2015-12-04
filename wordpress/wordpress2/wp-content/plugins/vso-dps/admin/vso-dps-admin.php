<?php
    function vso_dps_admin_html_page() {
?>
<div class="wrap">
    <h2>DPS Web Experience for WordPress</h2>

    <form method="post" action="options.php">
        <?php wp_nonce_field('update-options'); ?>

        <table class="form-table">
            <tr>
                <th scope="row">
                    <label for="vso_dps_storage_connection_string">Azure Storage Connection String</label></th>
                <td>
                    <input name="vso_dps_storage_connection_string" type="text" class="large-text" id="vso_dps_storage_connection_string" value="<?php echo get_option('vso_dps_storage_connection_string'); ?>" />
                </td>
            </tr>
            <tr>
                <th scope="row">
                    <label for="vso_dps_storage_container_name">Azure Storage Container Name</label></th>
                <td>
                    <input name="vso_dps_storage_container_name" type="text" class="regular-text ltr" id="vso_dps_storage_container_name" value="<?php echo get_option('vso_dps_storage_container_name'); ?>" />
                </td>
            </tr>
        </table>

        <input type="hidden" name="action" value="update" />
        <input type="hidden" name="page_options" value="vso_dps_storage_connection_string, vso_dps_storage_container_name" />

        <p>
            <input type="submit" value="<?php _e('Save Changes') ?>" />
        </p>

    </form>
</div>
<?php
    }
?>