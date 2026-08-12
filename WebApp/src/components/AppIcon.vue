<script setup>
defineProps({
  name: { type: String, required: true },
  size: { type: Number, default: 20 },
})
</script>

<template>
  <svg
    :width="size"
    :height="size"
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    stroke-width="1.8"
    stroke-linecap="round"
    stroke-linejoin="round"
    aria-hidden="true"
  >
    <template v-if="name === 'home'">
      <path d="m3 10 9-7 9 7" />
      <path d="M5 9v11h14V9M9 20v-6h6v6" />
    </template>
    <template v-else-if="name === 'versions'">
      <path d="M5 4h14v5H5zM5 15h14v5H5z" />
      <path d="M8 9v6M16 9v6" />
    </template>
    <template v-else-if="name === 'guides'">
      <path d="M5 4h14v16H5z" />
      <path d="M8 8h8M8 12h8M8 16h5" />
    </template>
    <template v-else-if="name === 'play'">
      <path d="M8 5.5v13l11-6.5z" />
    </template>
    <template v-else-if="name === 'download'">
      <path d="M12 3v12m0 0 4-4m-4 4-4-4" />
      <path d="M4 19h16" />
    </template>
    <template v-else-if="name === 'external'">
      <path d="M14 4h6v6M20 4l-9 9" />
      <path d="M18 13v6H5V6h6" />
    </template>
    <template v-else-if="name === 'menu'">
      <path d="M4 7h16M4 12h16M4 17h16" />
    </template>
    <template v-else-if="name === 'close'">
      <path d="m6 6 12 12M18 6 6 18" />
    </template>
    <template v-else-if="name === 'check'">
      <path d="m5 12 4 4L19 6" />
    </template>
    <template v-else-if="name === 'alert'">
      <path d="M12 4 3.5 19h17zM12 9v4M12 16h.01" />
    </template>
    <template v-else-if="name === 'clock'">
      <circle cx="12" cy="12" r="9" />
      <path d="M12 7v5l3 2" />
    </template>
    <template v-else>
      <circle cx="12" cy="12" r="9" />
    </template>
  </svg>
</template>
