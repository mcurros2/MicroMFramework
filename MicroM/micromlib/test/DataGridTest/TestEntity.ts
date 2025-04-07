import { Entity, EntityDefinition, MicroMClient } from "../../src"

export class TestEntityDefinition extends EntityDefinition {
    constructor() {
        super("TestEntity");
    }
}

export class TestEntity extends Entity<TestEntityDefinition> {
    constructor() {
        super(new MicroMClient({ app_id: "", api_url: "" }), new TestEntityDefinition())
        
    }
}