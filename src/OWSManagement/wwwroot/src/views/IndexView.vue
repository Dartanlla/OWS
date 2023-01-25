<template>
    <section class="d-flex align-items-center">
        <div class="container d-flex flex-column justify-content-center align-items-center text-center position-relative">
            <img src="@/assets/logo.png" class="img-fluid">
            <h2>OWSManagement</h2>
            <p><a href="http://www.sabredartstudios.com/">By SabreDartStudios</a></p>
            <div class="row">
                <div class="col">
                    <a href="https://www.openworldserver.com/" class="btn btn-primary text-nowrap" role="button"><i class="bi bi-archive"></i> Documentation</a>
                </div>
                <div class="col">
                    <a href="https://www.youtube.com/watch?v=yRXzbaNT6_k" class="btn btn-danger text-nowrap" role="button"><i class="bi bi-youtube"></i> Watch Tutorials</a>
                </div>
            </div> 
        </div>
</section>
</template>

<script lang="ts">
    import { defineComponent } from "vue";
    export default defineComponent({
        name: "IndexView",
    })
</script>

<style>
</style>