<template>
    <div class="button-wrapper">
        <button @click="setRandomItem" class="game-button">
            {{ item ?? "Click me for some fun!" }}
        </button>
    </div>
</template>

<script>
import items from './data/items.json';

export default {
    data() {
        return {
            items: items,
            currentItem: null,
            selectedItems: []
        }
    },
    methods: {
        setRandomItem() {
            var targetCurrentItem = Math.floor(Math.random() * this.items.length);
            if (this.selectedItems.includes(targetCurrentItem) && this.selectedItems.length < this.items.length) {
                this.setRandomItem();
            }
            else {
                this.currentItem = targetCurrentItem;
                this.selectedItems.push(this.currentItem);
            }
        }
    },
    computed: {
        item() {
            return this.currentItem >= 0 ? this.items[this.currentItem] : null;
        }
    }
}
</script>

<style scope>
.button-wrapper button
{
    font-size: x-large;
    font-weight: bolder;
    background-color: #04AA6D; /* Green */
    border: none;
    color: white;
    padding: 15px 32px;
}
</style>